using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class ExamResult : BaseEntity
{
    // Foreign Keys
    public int StudentId { get; set; }
    public int ExamId { get; set; }
    
    public decimal ObtainedMarks { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal Percentage => TotalMarks > 0 ? (ObtainedMarks / TotalMarks) * 100 : 0;
    
    [MaxLength(10)]
    public string Grade { get; set; } = string.Empty; // A+, A, B+, B, C+, C, D, F
    
    [MaxLength(20)]
    public string Result { get; set; } = string.Empty; // Pass, Fail, Absent
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    public string? EnteredBy { get; set; }
    public DateTime? EnteredDate { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
    
    [ForeignKey(nameof(ExamId))]
    public virtual Exam Exam { get; set; } = null!;
    
    [ForeignKey(nameof(EnteredBy))]
    public virtual ApplicationUser? EnteredByUser { get; set; }
}