using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Batch : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string BatchCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string BatchName { get; set; } = string.Empty;
    
    // Foreign Keys
    public int TradeId { get; set; }
    public int SessionId { get; set; }
    public int? TimingId { get; set; }
    public int? RoomId { get; set; }
    public int? PrimaryInstructorId { get; set; }
    public int? SecondaryInstructorId { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public int MaxStudents { get; set; }
    public int CurrentEnrollment { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    // Convenience property
    public string Name => BatchName;
    
    // Navigation Properties
    [ForeignKey(nameof(TradeId))]
    public virtual Trade Trade { get; set; } = null!;
    
    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;
    
    [ForeignKey(nameof(TimingId))]
    public virtual Timing? Timing { get; set; }
    
    [ForeignKey(nameof(RoomId))]
    public virtual Room? Room { get; set; }
    
    [ForeignKey(nameof(PrimaryInstructorId))]
    public virtual Teacher? PrimaryInstructor { get; set; }
    
    [ForeignKey(nameof(SecondaryInstructorId))]
    public virtual Teacher? SecondaryInstructor { get; set; }
    
    public virtual ICollection<BatchStudent> Students { get; set; } = new List<BatchStudent>();
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}