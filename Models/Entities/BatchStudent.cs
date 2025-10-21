using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class BatchStudent : BaseEntity
{
    public int BatchId { get; set; }
    public int StudentId { get; set; }
    
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Withdrawn, Completed
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(BatchId))]
    public virtual Batch Batch { get; set; } = null!;
    
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
}