using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class FeeStructure : BaseEntity
{
    [Required]
    public int TradeId { get; set; }
    
    [Required]
    public int SessionId { get; set; }
    
    // Gender-based fees
    public decimal MaleFee { get; set; }
    public decimal FemaleFee { get; set; }
    public decimal TransgenderFee { get; set; }
    
    // Alternative single fee (for trades with same fee for all genders)
    public decimal? StandardFee { get; set; }
    
    // Fee components breakdown
    public decimal? TuitionFee { get; set; }
    public decimal? AdmissionFee { get; set; }
    public decimal? SecurityFee { get; set; }
    public decimal? ExamFee { get; set; }
    public decimal? CertificateFee { get; set; }
    public decimal? LabFee { get; set; }
    public decimal? MaterialFee { get; set; }
    
    // Effective date range
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(200)]
    public string? FeeCategory { get; set; } // e.g., "Regular", "Vocational", "Technical"
    
    // Navigation Properties
    [ForeignKey(nameof(TradeId))]
    public virtual Trade Trade { get; set; } = null!;
    
    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;
    
    public virtual ICollection<StudentFeeAdjustment> StudentFeeAdjustments { get; set; } = new List<StudentFeeAdjustment>();
}