using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedDate { get; set; }
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(100)]
    public string? ModifiedBy { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsDeleted { get; set; } = false;
}