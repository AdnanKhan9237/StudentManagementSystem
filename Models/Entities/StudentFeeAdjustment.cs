using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class StudentFeeAdjustment : BaseEntity
{
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    public int FeeStructureId { get; set; }
    
    // Adjustment types
    public AdjustmentType AdjustmentType { get; set; }
    
    // Amount-based adjustments
    public decimal? AdjustmentAmount { get; set; }
    
    // Percentage-based adjustments
    public decimal? AdjustmentPercentage { get; set; }
    
    // Special cases
    public bool IsFree { get; set; } = false;
    
    [MaxLength(100)]
    public string? ConcessionType { get; set; } // e.g., "Government Employee", "Military", "Disability", "Merit"
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(200)]
    public string? ApprovedBy { get; set; }
    
    public DateTime ApprovedDate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    [MaxLength(200)]
    public string? SupportingDocuments { get; set; } // File paths or document references
    
    [MaxLength(1000)]
    public string? Remarks { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
    
    [ForeignKey(nameof(FeeStructureId))]
    public virtual FeeStructure FeeStructure { get; set; } = null!;
    
    // Calculated Properties
    [NotMapped]
    public decimal CalculatedAdjustment
    {
        get
        {
            if (IsFree) return 0; // Complete waiver
            
            if (AdjustmentType == AdjustmentType.FixedAmount)
                return AdjustmentAmount ?? 0;
            
            // For percentage-based, this would need the base fee amount
            return 0;
        }
    }
}

public enum AdjustmentType
{
    FixedAmount = 1,
    Percentage = 2,
    CompleteWaiver = 3
}