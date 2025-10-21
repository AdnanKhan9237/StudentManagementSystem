using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Certificate : BaseEntity
{
    // Foreign Keys
    public int StudentId { get; set; }
    public int TradeId { get; set; }
    public int SessionId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CertificateNumber { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string CertificateType { get; set; } = string.Empty; // Completion, Achievement, Participation
    
    public DateTime IssueDate { get; set; }
    
    public decimal? ObtainedMarks { get; set; }
    public decimal? TotalMarks { get; set; }
    public decimal? Percentage => TotalMarks > 0 && ObtainedMarks.HasValue ? (ObtainedMarks.Value / TotalMarks.Value) * 100 : null;
    
    [MaxLength(10)]
    public string? Grade { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Revoked, Cancelled
    
    [MaxLength(100)]
    public string? CertificatePath { get; set; } // Path to generated certificate PDF
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    public string? IssuedBy { get; set; }
    public DateTime? PrintedDate { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
    
    [ForeignKey(nameof(TradeId))]
    public virtual Trade Trade { get; set; } = null!;
    
    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;
    
    [ForeignKey(nameof(IssuedBy))]
    public virtual ApplicationUser? IssuedByUser { get; set; }
}