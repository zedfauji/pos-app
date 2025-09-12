using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using MagiDesk.Frontend.Collections;
using System.Text;
using System.IO;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class UnsettledBillsViewModel : INotifyPropertyChanged
    {
        private readonly TableRepository _tableRepository;
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string _searchTerm = string.Empty;
        private string _tableFilter = string.Empty;
        private string _serverFilter = string.Empty;
        private string _amountRangeFilter = string.Empty;
        
        // CRITICAL FIX: Use SafeObservableCollection to avoid COM interop issues
        // SafeObservableCollection suppresses COM exceptions that cause Marshal.ThrowExceptionForHR errors
        public SafeObservableCollection<BillResult> UnsettledBills { get; } = new();
        public SafeObservableCollection<BillResult> FilteredBills { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string TableFilter
        {
            get => _tableFilter;
            set
            {
                _tableFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string ServerFilter
        {
            get => _serverFilter;
            set
            {
                _serverFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string AmountRangeFilter
        {
            get => _amountRangeFilter;
            set
            {
                _amountRangeFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public decimal TotalUnsettledAmount
        {
            get => UnsettledBills.Sum(bill => bill.TotalAmount);
        }

        public decimal AverageAmount
        {
            get => UnsettledBills.Count > 0 ? UnsettledBills.Average(bill => bill.TotalAmount) : 0;
        }

        public int OverdueCount
        {
            get
            {
                var cutoffDate = DateTime.Now.AddDays(-7); // Consider bills overdue after 7 days
                return UnsettledBills.Count(bill => bill.StartTime < cutoffDate);
            }
        }

        public UnsettledBillsViewModel(TableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task LoadUnsettledBillsAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;

                System.Diagnostics.Debug.WriteLine("UnsettledBillsViewModel: Loading unsettled bills...");
                var bills = await _tableRepository.GetBillsAsync();
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Received {bills.Count} bills from TableRepository");
                
                // CRITICAL FIX: Use SafeObservableCollection to avoid COM interop issues
                // SafeObservableCollection suppresses COM exceptions that cause Marshal.ThrowExceptionForHR errors
                UnsettledBills.Clear();
                foreach (var bill in bills)
                {
                    UnsettledBills.Add(bill);
                    System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Added bill {bill.BillId} - ${bill.TotalAmount:F2}");
                }
                
                ApplyFilters();
                
                // Notify that statistics have changed
                OnPropertyChanged(nameof(TotalUnsettledAmount));
                OnPropertyChanged(nameof(AverageAmount));
                OnPropertyChanged(nameof(OverdueCount));
                
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Load completed - {UnsettledBills.Count} bills loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Error loading bills - {ex.Message}");
                HasError = true;
                ErrorMessage = $"Failed to load unsettled bills: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = UnsettledBills.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(bill => 
                    bill.BillId.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    bill.TableLabel.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    bill.ServerName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // Apply table filter
            if (!string.IsNullOrWhiteSpace(TableFilter))
            {
                filtered = filtered.Where(bill => bill.TableLabel.Equals(TableFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply server filter
            if (!string.IsNullOrWhiteSpace(ServerFilter))
            {
                filtered = filtered.Where(bill => bill.ServerName.Equals(ServerFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply amount range filter
            if (!string.IsNullOrWhiteSpace(AmountRangeFilter))
            {
                filtered = AmountRangeFilter switch
                {
                    "under50" => filtered.Where(bill => bill.TotalAmount < 50),
                    "50-100" => filtered.Where(bill => bill.TotalAmount >= 50 && bill.TotalAmount <= 100),
                    "100-200" => filtered.Where(bill => bill.TotalAmount > 100 && bill.TotalAmount <= 200),
                    "over200" => filtered.Where(bill => bill.TotalAmount > 200),
                    _ => filtered
                };
            }

            FilteredBills.Clear();
            foreach (var bill in filtered)
            {
                FilteredBills.Add(bill);
            }
        }

        public async Task ExportToCsvAsync()
        {
            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Bill ID,Table,Server,Amount,Start Time,Duration,Status");

                foreach (var bill in FilteredBills)
                {
                    var duration = DateTime.Now - bill.StartTime;
                    var status = bill.StartTime < DateTime.Now.AddDays(-7) ? "Overdue" : "Pending";
                    
                    csv.AppendLine($"{bill.BillId},{bill.TableLabel},{bill.ServerName},{bill.TotalAmount:F2},{bill.StartTime:yyyy-MM-dd HH:mm},{duration.TotalHours:F1} hours,{status}");
                }

                var fileName = $"UnsettledBills_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                await File.WriteAllTextAsync(filePath, csv.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export CSV: {ex.Message}", ex);
            }
        }

        public async Task ExportToExcelAsync()
        {
            try
            {
                // For now, create a CSV file with .xlsx extension
                // In a real implementation, you would use a library like EPPlus or ClosedXML
                var csv = new StringBuilder();
                csv.AppendLine("Bill ID,Table,Server,Amount,Start Time,Duration,Status");

                foreach (var bill in FilteredBills)
                {
                    var duration = DateTime.Now - bill.StartTime;
                    var status = bill.StartTime < DateTime.Now.AddDays(-7) ? "Overdue" : "Pending";
                    
                    csv.AppendLine($"{bill.BillId},{bill.TableLabel},{bill.ServerName},{bill.TotalAmount:F2},{bill.StartTime:yyyy-MM-dd HH:mm},{duration.TotalHours:F1} hours,{status}");
                }

                var fileName = $"UnsettledBills_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                await File.WriteAllTextAsync(filePath, csv.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export Excel: {ex.Message}", ex);
            }
        }

        public async Task PrintBillAsync(BillResult bill)
        {
            try
            {
                // This would integrate with your existing PDFSharp printing service
                // For now, we'll just simulate the print operation
                await Task.Delay(1000); // Simulate print delay
                
                // In a real implementation, you would call your printing service here
                // await _printingService.PrintBillAsync(bill);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to print bill: {ex.Message}", ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}