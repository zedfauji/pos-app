namespace MagiDesk.Shared.DTOs.Tables;

public class TableLayoutDto
{
    public Guid TableId { get; set; }
    public Guid FloorId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string TableType { get; set; } = "billiard"; // billiard | bar
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double Rotation { get; set; } // degrees 0-360
    public string Size { get; set; } = "M"; // S | M | L
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string Status { get; set; } = "available"; // available | occupied
    public decimal BillingRate { get; set; } // per hour
    public bool AutoStartTimer { get; set; }
    public string? IconStyle { get; set; }
    public List<string> GroupingTags { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } // lock position in designer
    public string? OrderId { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public string? Server { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTableLayoutRequest
{
    public Guid FloorId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string TableType { get; set; } = "billiard";
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double Rotation { get; set; } = 0;
    public string Size { get; set; } = "M";
    public double? Width { get; set; }
    public double? Height { get; set; }
    public decimal BillingRate { get; set; } = 0;
    public bool AutoStartTimer { get; set; } = false;
    public string? IconStyle { get; set; }
    public List<string>? GroupingTags { get; set; }
}

public class UpdateTableLayoutRequest
{
    public string? TableName { get; set; }
    public string? TableNumber { get; set; }
    public string? TableType { get; set; }
    public double? XPosition { get; set; }
    public double? YPosition { get; set; }
    public double? Rotation { get; set; }
    public string? Size { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string? Status { get; set; }
    public decimal? BillingRate { get; set; }
    public bool? AutoStartTimer { get; set; }
    public string? IconStyle { get; set; }
    public List<string>? GroupingTags { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLocked { get; set; }
}

public class FloorLayoutDto
{
    public FloorDto Floor { get; set; } = new();
    public List<TableLayoutDto> Tables { get; set; } = new();
}

public class LayoutSettingsDto
{
    public int GridSize { get; set; } = 20;
    public bool SnapToGrid { get; set; } = true;
    public string DefaultTableSize { get; set; } = "M";
    public bool ShowTableNumbers { get; set; } = true;
    public bool FloorSwitchLock { get; set; } = false;
    public bool ProtectLayoutChanges { get; set; } = false;
}

