using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? CNIC { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    [MaxLength(10)]
    public string? Gender { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? PhotoPath { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedDate { get; set; }
    
    [MaxLength(100)]
    public string? ModifiedBy { get; set; }
    
    public DateTime? LastLoginDate { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Require password change on first login (for provisioned accounts)
    public bool MustChangePassword { get; set; } = false;
    
    [MaxLength(50)]
    public string? Department { get; set; }
    
    [MaxLength(50)]
    public string? Designation { get; set; }
    
    [MaxLength(500)]
    public string? Qualifications { get; set; }
    
    // Full Name property for convenience
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual ICollection<Batch> InstructorBatches { get; set; } = new List<Batch>();
    public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
}