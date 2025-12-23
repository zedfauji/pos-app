using System;
using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Tables
{
    public class CreateTableRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public int TypeId { get; set; }
        
        public int Capacity { get; set; } = 4;
        
        public string Location { get; set; } = string.Empty;
    }

    public class UpdateTableRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public int TypeId { get; set; }
        
        public int Capacity { get; set; }
        
        public string Location { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class UpdateTableTypeRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, 1000)]
        public decimal HourlyRate { get; set; }
        
        public bool HasTimer { get; set; }
        
        public bool RequiresServer { get; set; }
        
        public bool AllowOrders { get; set; }
    }
}
