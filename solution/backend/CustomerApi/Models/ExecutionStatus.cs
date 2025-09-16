namespace CustomerApi.Models;

public enum ExecutionStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Opened = 4,
    Clicked = 5,
    Failed = 6,
    Completed = 7
}
