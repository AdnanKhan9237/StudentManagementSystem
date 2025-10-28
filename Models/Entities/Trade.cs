using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class Trade : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string NameEnglish { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string NameUrdu { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? DescriptionEnglish { get; set; }
    
    [MaxLength(500)]
    public string? DescriptionUrdu { get; set; }
    
    public int Duration { get; set; } // Duration in months
    
    public decimal TotalFee { get; set; }
    
    public int MaxStudents { get; set; }
    
    // Navigation properties
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    // New: enrollments per trade
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
