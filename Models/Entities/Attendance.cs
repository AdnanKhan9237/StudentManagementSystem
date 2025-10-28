using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Attendance : BaseEntity
{
    // Foreign Keys
    public int StudentId { get; set; }
    public int BatchId { get; set; }

    // New: Link attendance to a specific enrollment (session+trade)
    public int? EnrollmentId { get; set; }
    
    public DateTime Date { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // Present, Absent, Late, Excused
    
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    public string? MarkedBy { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
    
    [ForeignKey(nameof(BatchId))]
    public virtual Batch Batch { get; set; } = null!;

    [ForeignKey(nameof(EnrollmentId))]
    public virtual Enrollment? Enrollment { get; set; }
    
    [ForeignKey(nameof(MarkedBy))]
    public virtual ApplicationUser? MarkedByUser { get; set; }
}
