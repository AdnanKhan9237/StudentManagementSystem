using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class CertificationFeeConfig : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string QualificationLevel { get; set; } = string.Empty; // UnderMatric, Matric, Intermediate, AboveIntermediate
    
    [Required]
    public decimal FeeAmount { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public new bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; }
}
