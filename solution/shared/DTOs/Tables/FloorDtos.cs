namespace MagiDesk.Shared.DTOs.Tables;

public class FloorDto
{
    public Guid FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TableCount { get; set; } // Computed property
}

public class CreateFloorRequest
{
    public string FloorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateFloorRequest
{
    public string? FloorName { get; set; }
    public string? Description { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
}

public class DuplicateFloorRequest
{
    public string NewFloorName { get; set; } = string.Empty;
    public bool CopyTables { get; set; } = true;
}

