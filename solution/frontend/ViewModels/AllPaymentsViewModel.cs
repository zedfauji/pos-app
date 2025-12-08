using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using System.Text;
using System.IO;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class AllPaymentsViewModel : INotifyPropertyChanged
    {
        private readonly PaymentApiService _paymentService;
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string? _debugInfo;
        private bool _showDebugInfo = true;

        public ObservableCollection<PaymentApiService.PaymentDto> Payments { get; } = new();
        public ObservableCollection<PaymentApiService.PaymentDto> FilteredPayments { get; } = new();
        private Dictionary<Guid, decimal> _refundedAmounts = new(); // PaymentId -> TotalRefundedAmount
        private Dictionary<Guid, IReadOnlyList<PaymentApiService.RefundDto>> _refundsByPayment = new();

        private string _searchTerm = string.Empty;
        private string _paymentMethodFilter = string.Empty;
        private string _dateRangeFilter = string.Empty;
        private string _amountRangeFilter = string.Empty;

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

        public string? DebugInfo
        {
            get => _debugInfo;
            private set
            {
                _debugInfo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDebugInfo
        {
            get => _showDebugInfo;
            set
            {
                _showDebugInfo = value;
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

        public string PaymentMethodFilter
        {
            get => _paymentMethodFilter;
            set
            {
                _paymentMethodFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string DateRangeFilter
        {
            get => _dateRangeFilter;
            set
            {
                _dateRangeFilter = value;
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

        public decimal TotalAmount => Payments.Sum(p => p.AmountPaid);
        public decimal AverageAmount => Payments.Count > 0 ? Payments.Average(p => p.AmountPaid) : 0;
        public int CashCount => Payments.Count(p => p.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase));
        public int CardCount => Payments.Count(p => p.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase));

        public AllPaymentsViewModel(PaymentApiService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task LoadPaymentsAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;

                // Request a large limit to get all payments (backend max is 1000, so we'll request that)
                var payments = await _paymentService.GetAllPaymentsAsync(limit: 1000);
                
                // Debug: Show info in UI
                DebugInfo = $"API returned {payments.Count} payments";
                
                Payments.Clear();
                _refundedAmounts.Clear();
                _refundsByPayment.Clear();
                
                foreach (var payment in payments)
                {
                    Payments.Add(payment);
                    // Load refunds for each payment (async, but don't await to avoid blocking)
                    _ = LoadRefundsForPaymentAsync(payment.PaymentId);
                }
                
                // Update debug info with payment details
                if (payments.Count > 0)
                {
                    var paymentDetails = string.Join(", ", payments.Take(3).Select(p => $"ID:{p.PaymentId} ${p.AmountPaid:F2}"));
                    DebugInfo = $"Loaded {payments.Count} payments: {paymentDetails}" + (payments.Count > 3 ? "..." : "");
                }
                else
                {
                    DebugInfo = "No payments found in API response";
                }
                
                ApplyFilters();
                
                // Notify that statistics have changed
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(AverageAmount));
                OnPropertyChanged(nameof(CashCount));
                OnPropertyChanged(nameof(CardCount));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load payments: {ex.Message}";
                DebugInfo = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = Payments.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(p => 
                    p.PaymentId.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.SessionId.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.PaymentMethod.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.CreatedBy.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // Apply payment method filter
            if (!string.IsNullOrWhiteSpace(PaymentMethodFilter))
            {
                filtered = filtered.Where(p => p.PaymentMethod.Equals(PaymentMethodFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply date range filter
            if (!string.IsNullOrWhiteSpace(DateRangeFilter))
            {
                var now = DateTimeOffset.Now;
                filtered = DateRangeFilter switch
                {
                    "today" => filtered.Where(p => p.CreatedAt.Date == now.Date),
                    "week" => filtered.Where(p => p.CreatedAt >= now.AddDays(-7)),
                    "month" => filtered.Where(p => p.CreatedAt >= now.AddDays(-30)),
                    "quarter" => filtered.Where(p => p.CreatedAt >= now.AddDays(-90)),
                    _ => filtered
                };
            }

            // Apply amount range filter
            if (!string.IsNullOrWhiteSpace(AmountRangeFilter))
            {
                filtered = AmountRangeFilter switch
                {
                    "under25" => filtered.Where(p => p.AmountPaid < 25),
                    "25-50" => filtered.Where(p => p.AmountPaid >= 25 && p.AmountPaid <= 50),
                    "50-100" => filtered.Where(p => p.AmountPaid > 50 && p.AmountPaid <= 100),
                    "100-200" => filtered.Where(p => p.AmountPaid > 100 && p.AmountPaid <= 200),
                    "over200" => filtered.Where(p => p.AmountPaid > 200),
                    _ => filtered
                };
            }

            FilteredPayments.Clear();
            foreach (var payment in filtered)
            {
                FilteredPayments.Add(payment);
            }
        }

        public void ClearFilters()
        {
            SearchTerm = string.Empty;
            PaymentMethodFilter = string.Empty;
            DateRangeFilter = string.Empty;
            AmountRangeFilter = string.Empty;
        }

        public async Task ExportToCsvAsync()
        {
            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Payment ID,Amount,Method,Session ID,Created At,Created By");

                foreach (var payment in FilteredPayments)
                {
                    csv.AppendLine($"{payment.PaymentId},{payment.AmountPaid:F2},{payment.PaymentMethod},{payment.SessionId},{payment.CreatedAt:yyyy-MM-dd HH:mm},{payment.CreatedBy}");
                }

                var fileName = $"AllPayments_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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
                csv.AppendLine("Payment ID,Amount,Method,Session ID,Created At,Created By");

                foreach (var payment in FilteredPayments)
                {
                    csv.AppendLine($"{payment.PaymentId},{payment.AmountPaid:F2},{payment.PaymentMethod},{payment.SessionId},{payment.CreatedAt:yyyy-MM-dd HH:mm},{payment.CreatedBy}");
                }

                var fileName = $"AllPayments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                await File.WriteAllTextAsync(filePath, csv.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export Excel: {ex.Message}", ex);
            }
        }

        public async Task<PaymentApiService.RefundDto> ProcessRefundAsync(Guid paymentId, decimal refundAmount, string refundReason, string refundMethod = "original")
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;

                // Validate refund amount
                var payment = Payments.FirstOrDefault(p => p.PaymentId == paymentId);
                if (payment == null)
                    throw new Exception("Payment not found");

                // Get existing refunds for this payment
                var existingRefunds = await _paymentService.GetRefundsByPaymentIdAsync(paymentId);
                var totalRefunded = existingRefunds.Sum(r => r.RefundAmount);
                var remainingAmount = payment.AmountPaid - totalRefunded;

                if (refundAmount <= 0)
                    throw new Exception("Refund amount must be greater than zero");

                if (refundAmount > remainingAmount)
                    throw new Exception($"Refund amount ({refundAmount:C}) exceeds remaining refundable amount ({remainingAmount:C})");

                // Validate refund reason is required
                if (string.IsNullOrWhiteSpace(refundReason))
                    throw new Exception("Refund reason is required. Please provide a reason for the refund.");

                // Process refund
                var request = new PaymentApiService.ProcessRefundRequestDto(
                    refundAmount,
                    refundReason,
                    refundMethod
                );

                var refund = await _paymentService.ProcessRefundAsync(paymentId, request);
                
                if (refund == null)
                    throw new Exception("Refund processing returned no result");

                // Refresh payments list to get updated data
                await LoadPaymentsAsync();

                return refund;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to process refund: {ex.Message}";
                throw new Exception($"Failed to process refund: {ex.Message}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<IReadOnlyList<PaymentApiService.RefundDto>> GetRefundsForPaymentAsync(Guid paymentId)
        {
            try
            {
                if (_refundsByPayment.TryGetValue(paymentId, out var cached))
                    return cached;
                
                var refunds = await _paymentService.GetRefundsByPaymentIdAsync(paymentId);
                _refundsByPayment[paymentId] = refunds;
                var totalRefunded = refunds.Sum(r => r.RefundAmount);
                _refundedAmounts[paymentId] = totalRefunded;
                return refunds;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get refunds: {ex.Message}", ex);
            }
        }

        private async Task LoadRefundsForPaymentAsync(Guid paymentId)
        {
            try
            {
                var refunds = await _paymentService.GetRefundsByPaymentIdAsync(paymentId);
                _refundsByPayment[paymentId] = refunds;
                var totalRefunded = refunds.Sum(r => r.RefundAmount);
                _refundedAmounts[paymentId] = totalRefunded;
            }
            catch
            {
                // Silently fail - refunds will be loaded on demand
            }
        }

        public decimal GetRefundedAmount(Guid paymentId)
        {
            return _refundedAmounts.TryGetValue(paymentId, out var amount) ? amount : 0m;
        }

        public bool HasRefunds(Guid paymentId)
        {
            return _refundedAmounts.TryGetValue(paymentId, out var amount) && amount > 0;
        }

        public bool IsFullyRefunded(Guid paymentId)
        {
            var payment = Payments.FirstOrDefault(p => p.PaymentId == paymentId);
            if (payment == null) return false;
            var refunded = GetRefundedAmount(paymentId);
            return refunded >= payment.AmountPaid;
        }

        public async Task PrintReceiptAsync(PaymentApiService.PaymentDto payment)
        {
            try
            {
                // This would integrate with your existing PDFSharp printing service
                // For now, we'll just simulate the print operation
                await Task.Delay(1000); // Simulate print delay
                
                // In a real implementation, you would call your printing service here
                // await _printingService.PrintReceiptAsync(payment);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to print receipt: {ex.Message}", ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}