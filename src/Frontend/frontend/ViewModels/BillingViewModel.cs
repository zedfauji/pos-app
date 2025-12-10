using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Npgsql;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class BillingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly TableRepository _tableRepository;
        private readonly BillingService _billingService;
        private readonly ILogger<BillingViewModel>? _logger;
        private readonly DispatcherQueue _dispatcher;
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BillingViewModel(TableRepository tableRepository, ILogger<BillingViewModel>? logger = null)
        {
            _tableRepository = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
            _billingService = new BillingService();
            _logger = logger;
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            
            // Initialize collections
            Bills = new ObservableCollection<BillItemViewModel>();
            FilteredBills = new ObservableCollection<BillItemViewModel>();
            
            // Initialize commands
            RefreshCommand = new Services.RelayCommand(async _ => await LoadBillsAsync());
            ApplyFiltersCommand = new Services.RelayCommand(async _ => await ApplyFiltersAsync());
            ExportCommand = new Services.RelayCommand(async _ => await ExportDataAsync());
            ClearFiltersCommand = new Services.RelayCommand(_ => ClearFilters());
            
            // Initialize date ranges - set ToDate to end of tomorrow to include all bills including today and future bills
            FromDate = DateTimeOffset.Now.AddDays(-30);
            ToDate = DateTimeOffset.Now.Date.AddDays(2).AddTicks(-1); // End of tomorrow to include all bills
            
            // Load initial data
            _ = LoadBillsAsync();
        }

        #region Properties

        private ObservableCollection<BillItemViewModel> _bills = new();
        public ObservableCollection<BillItemViewModel> Bills
        {
            get => _bills;
            private set
            {
                _bills = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalBillsCount));
            }
        }

        private ObservableCollection<BillItemViewModel> _filteredBills = new();
        public ObservableCollection<BillItemViewModel> FilteredBills
        {
            get => _filteredBills;
            private set
            {
                _filteredBills = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredBillsCount));
                OnPropertyChanged(nameof(HasBills));
                OnPropertyChanged(nameof(HasEmptyState));
            }
        }

        public int TotalBillsCount => Bills.Count;
        public int FilteredBillsCount => FilteredBills.Count;

        private DateTimeOffset _fromDate;
        public DateTimeOffset FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset _toDate;
        public DateTimeOffset ToDate
        {
            get => _toDate;
            set
            {
                _toDate = value;
                OnPropertyChanged();
            }
        }

        private string _tableFilter = string.Empty;
        public string TableFilter
        {
            get => _tableFilter;
            set
            {
                _tableFilter = value;
                OnPropertyChanged();
            }
        }

        private string _serverFilter = string.Empty;
        public string ServerFilter
        {
            get => _serverFilter;
            set
            {
                _serverFilter = value;
                OnPropertyChanged();
            }
        }

        private string _statusFilter = "All";
        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasBills));
                OnPropertyChanged(nameof(HasEmptyState));
            }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(HasBills));
                OnPropertyChanged(nameof(HasEmptyState));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        
        public bool HasBills => !IsLoading && !HasError && FilteredBills.Count > 0;
        
        public bool HasEmptyState => !IsLoading && !HasError && FilteredBills.Count == 0;

        #endregion

        #region Analytics Properties

        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            private set
            {
                _totalRevenue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalRevenueText));
            }
        }

        public string TotalRevenueText => TotalRevenue.ToString("C");

        private decimal _averageBillAmount;
        public decimal AverageBillAmount
        {
            get => _averageBillAmount;
            private set
            {
                _averageBillAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AverageBillAmountText));
            }
        }

        public string AverageBillAmountText => AverageBillAmount.ToString("C");

        private int _totalSessions;
        public int TotalSessions
        {
            get => _totalSessions;
            private set
            {
                _totalSessions = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _averageSessionDuration;
        public TimeSpan AverageSessionDuration
        {
            get => _averageSessionDuration;
            private set
            {
                _averageSessionDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AverageSessionDurationText));
            }
        }

        public string AverageSessionDurationText => 
            AverageSessionDuration.TotalHours >= 1 
                ? $"{AverageSessionDuration.Hours}h {AverageSessionDuration.Minutes}m"
                : $"{AverageSessionDuration.Minutes}m";

        #endregion

        #region Commands

        public Services.RelayCommand RefreshCommand { get; }
        public Services.RelayCommand ApplyFiltersCommand { get; }
        public Services.RelayCommand ExportCommand { get; }
        public Services.RelayCommand ClearFiltersCommand { get; }

        #endregion

        #region Methods

        private async Task<List<BillResult>> GetBillingDataFromOrdersAsync()
        {
            _logger?.LogInformation("Getting billing data via API and aggregating order items");
            
            // Get basic billing data from API
            var bills = await _tableRepository.GetBillsAsync(
                FromDate, 
                ToDate, 
                string.IsNullOrWhiteSpace(TableFilter) ? null : TableFilter,
                string.IsNullOrWhiteSpace(ServerFilter) ? null : ServerFilter);

            _logger?.LogInformation("Retrieved {BillCount} bills from TableRepository API", bills.Count);

            // For each bill, get the detailed order items using BillingService
            _logger?.LogInformation("Starting to process {BillCount} bills for order items", bills.Count);
            
            foreach (var bill in bills)
            {
                _logger?.LogInformation("Processing bill: BillingId={BillingId}, TableLabel={TableLabel}, TotalAmount={TotalAmount}", 
                    bill.BillingId, bill.TableLabel, bill.TotalAmount);
                    
                if (bill.BillingId != null && bill.BillingId != Guid.Empty)
                {
                    try
                    {
                        _logger?.LogInformation("Calling BillingService.GetOrderItemsByBillingIdAsync for {BillingId}", bill.BillingId);
                        var orderItems = await _billingService.GetOrderItemsByBillingIdAsync(bill.BillingId.Value);
                        bill.Items = orderItems ?? new List<ItemLine>();
                        _logger?.LogInformation("Successfully added {ItemCount} items to billing {BillingId}. Items: {@Items}", 
                            bill.Items.Count, bill.BillingId, bill.Items.Take(3).Select(i => new { i.itemId, i.name, i.quantity, i.price }));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to get order items for billing {BillingId}: {ErrorMessage}", bill.BillingId, ex.Message);
                        bill.Items = new List<ItemLine>();
                    }
                }
                else
                {
                    _logger?.LogWarning("Bill has null or empty BillingId: {BillInfo}", new { bill.TableLabel, bill.TotalAmount, bill.StartTime });
                    bill.Items = new List<ItemLine>();
                }
            }
            
            _logger?.LogInformation("Finished processing all bills for order items");

            return bills;
        }

        public async Task LoadBillsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _logger?.LogInformation("Starting LoadBillsAsync - From: {FromDate} to {ToDate}", FromDate, ToDate);

                // Verify BillingService is initialized
                if (_billingService == null)
                {
                    throw new InvalidOperationException("BillingService is not initialized");
                }

                var connectionString = _billingService.GetConnectionString();
                _logger?.LogInformation("BillingService connection string length: {Length}", connectionString?.Length ?? 0);

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is empty or null");
                }

                // Get billing data directly from database by aggregating orders
                var bills = await GetBillingDataFromOrdersAsync();
                _logger?.LogInformation("Retrieved {BillCount} bills from database", bills.Count);

                _dispatcher.TryEnqueue(() =>
                {
                    Bills.Clear();
                    foreach (var bill in bills)
                    {
                        Bills.Add(new BillItemViewModel(bill));
                    }
                    
                    CalculateAnalytics();
                    ApplyFiltersInternal();
                    OnPropertyChanged(nameof(HasBills));
                    OnPropertyChanged(nameof(HasEmptyState));
                    _logger?.LogInformation("UI updated with {BillCount} bills", Bills.Count);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LoadBillsAsync failed - Exception: {ExceptionType}, Message: {ExceptionMessage}", 
                    ex.GetType().Name, ex.Message);
                _logger?.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                ErrorMessage = $"Failed to load bill details: {ex.Message}";
                
                // Also log inner exceptions if any
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    _logger?.LogError("Inner exception: {InnerType} - {InnerMessage}", innerEx.GetType().Name, innerEx.Message);
                    innerEx = innerEx.InnerException;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ApplyFiltersAsync()
        {
            try
            {
                _dispatcher.TryEnqueue(() =>
                {
                    ApplyFiltersInternal();
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to apply filters");
                ErrorMessage = $"Failed to apply filters: {ex.Message}";
            }
        }

        private void ApplyFiltersInternal()
        {
            var filtered = Bills.AsEnumerable();

            // Apply status filter
            if (StatusFilter != "All")
            {
                filtered = filtered.Where(b => 
                    StatusFilter == "Open" ? !b.IsClosed :
                    StatusFilter == "Closed" ? b.IsClosed :
                    true);
            }

            // Apply table filter
            if (!string.IsNullOrWhiteSpace(TableFilter))
            {
                filtered = filtered.Where(b => 
                    b.TableLabel.Contains(TableFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply server filter
            if (!string.IsNullOrWhiteSpace(ServerFilter))
            {
                filtered = filtered.Where(b => 
                    b.ServerName.Contains(ServerFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredBills.Clear();
            foreach (var bill in filtered.OrderByDescending(b => b.StartTime))
            {
                FilteredBills.Add(bill);
            }
            
            OnPropertyChanged(nameof(HasBills));
            OnPropertyChanged(nameof(HasEmptyState));
        }

        public void ClearFilters()
        {
            TableFilter = string.Empty;
            ServerFilter = string.Empty;
            StatusFilter = "All";
            _ = ApplyFiltersAsync();
        }

        private void CalculateAnalytics()
        {
            if (!Bills.Any()) return;

            TotalRevenue = Bills.Sum(b => b.Amount);
            AverageBillAmount = Bills.Average(b => b.Amount);
            TotalSessions = Bills.Count;
            
            var totalMinutes = Bills.Sum(b => b.TotalTimeMinutes);
            AverageSessionDuration = TimeSpan.FromMinutes(totalMinutes / (double)Bills.Count);
        }

        public async Task ExportDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _logger?.LogInformation("Exporting billing data to CSV");

                // Create CSV content
                var csvContent = GenerateCsvContent();
                
                // Save to file
                var fileName = $"BillingExport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                var filePath = System.IO.Path.Combine(downloadsPath, fileName);
                
                await System.IO.File.WriteAllTextAsync(filePath, csvContent);
                
                _logger?.LogInformation("Billing data exported to {FilePath}", filePath);
                
                // Show success message (this would typically be handled by the UI)
                ErrorMessage = $"Data exported successfully to: {fileName}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to export data");
                ErrorMessage = $"Failed to export data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GenerateCsvContent()
        {
            var csv = new System.Text.StringBuilder();
            
            // Add header
            csv.AppendLine("Bill ID,Table,Server,Duration (min),Start Time,End Time,Amount,Status");
            
            // Add data rows
            foreach (var bill in FilteredBills)
            {
                csv.AppendLine($"{bill.ShortBillId},{bill.TableLabel},{bill.ServerName}," +
                             $"{bill.TotalTimeMinutes},{bill.StartTime:yyyy-MM-dd HH:mm:ss}," +
                             $"{bill.EndTime:yyyy-MM-dd HH:mm:ss},{bill.Amount:F2},{bill.StatusText}");
            }
            
            // Add summary
            csv.AppendLine();
            csv.AppendLine("SUMMARY");
            csv.AppendLine($"Total Revenue,{TotalRevenue:F2}");
            csv.AppendLine($"Average Bill Amount,{AverageBillAmount:F2}");
            csv.AppendLine($"Total Sessions,{TotalSessions}");
            csv.AppendLine($"Average Duration (min),{AverageSessionDuration.TotalMinutes:F1}");
            
            return csv.ToString();
        }

        public async Task<BillResult?> GetBillDetailsAsync(Guid billId)
        {
            try
            {
                var bill = await _tableRepository.GetBillByIdAsync(billId);
                if (bill == null) return null;
                
                // Fetch order items if we have a billing ID
                if (bill.BillingId != null && bill.BillingId != Guid.Empty)
                {
                    try
                    {
                        _logger?.LogInformation("Fetching order items for bill details - BillingId: {BillingId}", bill.BillingId);
                        var orderItems = await _billingService.GetOrderItemsByBillingIdAsync(bill.BillingId.Value);
                        bill.Items = orderItems ?? new List<ItemLine>();
                        _logger?.LogInformation("Fetched {ItemCount} order items for bill details", bill.Items.Count);
                        
                        // Debug: Log item prices to see what we're getting
                        decimal calculatedTotal = 0;
                        foreach (var item in bill.Items)
                        {
                            var itemTotal = item.price * item.quantity;
                            calculatedTotal += itemTotal;
                            _logger?.LogInformation("Item: {Name}, Qty: {Qty}, Price: {Price}, Total: {Total}", 
                                item.name, item.quantity, item.price, itemTotal);
                        }
                        _logger?.LogInformation("Calculated items total: {CalculatedTotal}, Original bill total: {OriginalTotal}", 
                            calculatedTotal, bill.TotalAmount);
                        
                        // Update the bill totals with calculated values
                        bill.ItemsCost = calculatedTotal;
                        bill.TotalAmount = bill.ItemsCost + bill.TimeCost;
                        _logger?.LogInformation("Updated bill totals - ItemsCost: {ItemsCost}, TotalAmount: {TotalAmount}", 
                            bill.ItemsCost, bill.TotalAmount);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to fetch order items for bill details - BillingId: {BillingId}", bill.BillingId);
                        bill.Items = new List<ItemLine>();
                    }
                }
                else
                {
                    _logger?.LogWarning("Bill {BillId} has no BillingId, cannot fetch order items", billId);
                    bill.Items = new List<ItemLine>();
                }
                
                return bill;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get bill details for {BillId}", billId);
                ErrorMessage = $"Failed to get bill details: {ex.Message}";
                return null;
            }
        }

        public async Task<bool> CloseBillAsync(Guid billId)
        {
            try
            {
                var result = await _tableRepository.CloseBillAsync(billId);
                if (result)
                {
                    // Refresh the bill in our collection
                    var bill = Bills.FirstOrDefault(b => b.BillId == billId);
                    if (bill != null)
                    {
                        bill.IsClosed = true;
                        await ApplyFiltersAsync();
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to close bill {BillId}", billId);
                ErrorMessage = $"Failed to close bill: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #endregion
    }

    public sealed class BillItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public BillItemViewModel(BillResult bill)
        {
            // Use BillingId as the primary identifier, fall back to BillId if needed
            BillingId = bill.BillingId ?? bill.BillId;
            BillId = bill.BillId;
            TableLabel = bill.TableLabel;
            ServerName = bill.ServerName;
            TotalTimeMinutes = bill.TotalTimeMinutes;
            StartTime = bill.StartTime;
            EndTime = bill.EndTime;
            SessionId = bill.SessionId;
            Items = bill.Items ?? new List<ItemLine>();
            
            // Use TotalAmount from API response as primary source
            // Only calculate from items if TotalAmount is 0 or missing and we have items
            if (bill.TotalAmount > 0)
            {
                Amount = bill.TotalAmount;
            }
            else
            {
                // Fall back to calculating from items if TotalAmount is not available
                var calculatedTotal = CalculateTotal();
                Amount = calculatedTotal > 0 ? calculatedTotal : bill.TotalAmount;
            }
            
            // For now, assume bills are closed if they have an EndTime
            IsClosed = bill.EndTime != default(DateTime);
            
            // Session movement tracking
            OriginalTableLabel = bill.OriginalTableLabel ?? bill.TableLabel;
            HasSessionMoved = !string.IsNullOrEmpty(bill.OriginalTableLabel) && 
                             bill.OriginalTableLabel != bill.TableLabel;
        }

        private decimal CalculateTotal()
        {
            return Items?.Sum(item => item.price * item.quantity) ?? 0m;
        }

        public Guid BillingId { get; }
        public Guid BillId { get; }
        public Guid? SessionId { get; }
        public List<ItemLine> Items { get; }
        public string ShortBillingId => BillingId.ToString()[..8];
        public string ShortBillId => BillId.ToString()[..8];
        public string TableLabel { get; }
        public string ServerName { get; }
        public int TotalTimeMinutes { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public decimal Amount { get; }
        public bool IsClosed { get; set; }
        
        // Session movement tracking
        public string OriginalTableLabel { get; }
        public bool HasSessionMoved { get; }

        public string StartLocal => StartTime.ToLocalTime().ToString("MM/dd HH:mm");
        public string EndLocal => EndTime.ToLocalTime().ToString("MM/dd HH:mm");
        public string DurationText => TimeSpan.FromMinutes(TotalTimeMinutes).ToString(@"hh\:mm");
        public string AmountText => Amount.ToString("C");
        public string StatusText => IsClosed ? "Closed" : "Open";
        public string StatusColor => IsClosed ? "#107C10" : "#FF8C00";
        
        // Session movement display properties
        public string TableDisplayText => HasSessionMoved ? $"{OriginalTableLabel} → {TableLabel}" : TableLabel;
        public string MovementIndicator => HasSessionMoved ? "📍" : "";
        public string MovementTooltip => HasSessionMoved ? $"Session moved from {OriginalTableLabel} to {TableLabel}" : "";

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
