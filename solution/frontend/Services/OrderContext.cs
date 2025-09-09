using System;

namespace MagiDesk.Frontend.Services
{
    public static class OrderContext
    {
        private static long? _currentOrderId;
        public static event EventHandler<long?>? CurrentOrderChanged;
        private static string? _currentSessionId;
        public static event EventHandler<string?>? CurrentSessionChanged;
        private static string? _currentBillingId;
        public static event EventHandler<string?>? CurrentBillingChanged;

        public static long? CurrentOrderId
        {
            get => _currentOrderId;
            set
            {
                if (_currentOrderId != value)
                {
                    _currentOrderId = value;
                    try { CurrentOrderChanged?.Invoke(null, _currentOrderId); } catch { }
                }
            }
        }

        public static bool HasActiveOrder => _currentOrderId.HasValue;

        public static string? CurrentSessionId
        {
            get => _currentSessionId;
            set
            {
                if (!string.Equals(_currentSessionId, value, StringComparison.Ordinal))
                {
                    _currentSessionId = value;
                    try { CurrentSessionChanged?.Invoke(null, _currentSessionId); } catch { }
                }
            }
        }

        public static string? CurrentBillingId
        {
            get => _currentBillingId;
            set
            {
                if (!string.Equals(_currentBillingId, value, StringComparison.Ordinal))
                {
                    _currentBillingId = value;
                    try { CurrentBillingChanged?.Invoke(null, _currentBillingId); } catch { }
                }
            }
        }
    }
}
