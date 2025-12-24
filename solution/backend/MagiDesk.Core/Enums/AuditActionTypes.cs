namespace MagiDesk.Core.Enums
{
    public static class AuditActionTypes
    {
        // Financial
        public const string OrderCreated = "ORDER_CREATED";
        public const string OrderItemAdded = "ORDER_ITEM_ADDED";
        public const string OrderItemRemoved = "ORDER_ITEM_REMOVED";
        public const string PaymentReceived = "PAYMENT_RECEIVED";
        public const string RefundProcessed = "REFUND_PROCESSED";
        public const string DiscountApplied = "DISCOUNT_APPLIED";
        public const string BillGenerated = "BILL_GENERATED";

        // Operational
        public const string ShiftOpened = "SHIFT_OPENED";
        public const string ShiftClosed = "SHIFT_CLOSED";
        public const string SessionStarted = "SESSION_STARTED";
        public const string SessionStopped = "SESSION_STOPPED";
        public const string SessionMoved = "SESSION_MOVED";
        public const string TableReassigned = "TABLE_REASSIGNED";

        // Security
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string Logout = "LOGOUT";
        public const string PermissionChanged = "PERMISSION_CHANGED";
        public const string SettingsChanged = "SETTINGS_CHANGED";
    }
}
