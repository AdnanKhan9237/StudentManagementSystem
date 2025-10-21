using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class Room : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string RoomNumber { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? RoomName { get; set; }
    
    public int Capacity { get; set; }
    
    [MaxLength(50)]
    public string RoomType { get; set; } = string.Empty; // Classroom, Lab, Workshop, etc.
    
    [MaxLength(100)]
    public string? Building { get; set; }
    
    [MaxLength(50)]
    public string? Floor { get; set; }
    
    [MaxLength(500)]
    public string? Equipment { get; set; }
    
    public bool HasProjector { get; set; }
    
    public bool HasComputers { get; set; }
    
    public bool HasAirConditioning { get; set; }
    
    // Navigation properties
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
}