using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Core.Entities
{
    /// <summary>
    /// Plant Configuration model for FROM/TO plant database connections
    /// </summary>
    public class PlantConfiguration
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Plant Code is required")]
        [StringLength(50, ErrorMessage = "Plant Code cannot exceed 50 characters")]
        [Display(Name = "Plant Code")]
        public string PlantCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Plant Name is required")]
        [StringLength(100, ErrorMessage = "Plant Name cannot exceed 100 characters")]
        [Display(Name = "Plant Name")]
        public string PlantName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Plant Type is required")]
        [Display(Name = "Plant Type")]
        public string PlantType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Server IP is required")]
        [StringLength(100, ErrorMessage = "Server IP cannot exceed 100 characters")]
        [Display(Name = "Server IP / Hostname")]
        public string ServerIP { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Database Name is required")]
        [StringLength(100, ErrorMessage = "Database Name cannot exceed 100 characters")]
        [Display(Name = "Database Name")]
        public string DatabaseName { get; set; } = string.Empty;
        
        [StringLength(50)]
        [Display(Name = "Username")]
        public string? Username { get; set; }
        
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }
        
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        [Display(Name = "Port")]
        public int Port { get; set; } = 1433;
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Location")]
        public string? Location { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }
        
        [StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }
        
        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? CreatedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        [StringLength(50)]
        public string? ModifiedBy { get; set; }
        
        // Sync status fields
        public DateTime? LastSyncSuccess { get; set; }
        
        [StringLength(500)]
        public string? LastSyncStatus { get; set; }
    }
}
