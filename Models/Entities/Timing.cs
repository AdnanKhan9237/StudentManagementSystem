using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class Timing : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Morning, Evening, etc.
    
    public TimeSpan StartTime { get; set; }
    
    public TimeSpan EndTime { get; set; }
    
    [MaxLength(20)]
    public string Shift { get; set; } = string.Empty; // Morning, Evening, Night
    
    [MaxLength(50)]
    public string? Type { get; set; } // Morning, Afternoon, Evening, Night, Weekend
    
    [MaxLength(100)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
}