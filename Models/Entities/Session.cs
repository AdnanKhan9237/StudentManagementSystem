using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class Session : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    
public DateTime EndDate { get; set; }

    // Optional registration window
    public DateTime? RegistrationStartDate { get; set; }

    public DateTime? RegistrationEndDate { get; set; }
    
    public bool IsCurrentSession { get; set; }
    
    [MaxLength(50)]
    public string SessionType { get; set; } = "Regular"; // Regular, Short-Term, Long-Term
    
    public int DurationMonths { get; set; } = 6; // Default 6 months
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<Trade> Trades { get; set; } = new List<Trade>();
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    // New: enrollments per session
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
