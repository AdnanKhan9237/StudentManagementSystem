using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Exam : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string ExamName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string ExamType { get; set; } = string.Empty; // Monthly, Mid, Final, Practical
    
    // Foreign Keys
    public int SessionId { get; set; }
    public int? TradeId { get; set; } // Null for general exams
    
    public DateTime ExamDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public int Duration { get; set; } // Duration in minutes
    public decimal TotalMarks { get; set; }
    public decimal PassingMarks { get; set; }
    
    [MaxLength(100)]
    public string? Subject { get; set; }
    
    [MaxLength(500)]
    public string? Instructions { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, InProgress, Completed, Cancelled
    
    // Navigation Properties
    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;
    
    [ForeignKey(nameof(TradeId))]
    public virtual Trade? Trade { get; set; }
    
    public virtual ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
}