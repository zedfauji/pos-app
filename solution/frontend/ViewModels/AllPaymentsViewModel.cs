using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class AllPaymentsViewModel : INotifyPropertyChanged
    {
        private readonly PaymentApiService _paymentService;
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string? _debugInfo; // Add debug info property

        public ObservableCollection<PaymentApiService.PaymentDto> Payments { get; } = new();
        public ObservableCollection<PaymentApiService.PaymentDto> FilteredPayments { get; } = new();

        private string _searchTerm = string.Empty;
        private string _paymentMethodFilter = string.Empty;
        private string _dateRangeFilter = string.Empty;

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

        public decimal TotalAmount => Payments.Sum(p => p.AmountPaid);
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

                var payments = await _paymentService.GetAllPaymentsAsync();
                
                // Debug: Show info in UI
                DebugInfo = $"API returned {payments.Count} payments";
                
                Payments.Clear();
                foreach (var payment in payments)
                {
                    Payments.Add(payment);
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
                OnPropertyChanged(nameof(CashCount));
                OnPropertyChanged(nameof(CardCount));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load payments: {ex.Message}";
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
                    p.PaymentMethod.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
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
                    _ => filtered
                };
            }

            FilteredPayments.Clear();
            foreach (var payment in filtered)
            {
                FilteredPayments.Add(payment);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}