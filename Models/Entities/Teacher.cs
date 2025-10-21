using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Teacher : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string TeacherCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? FatherName { get; set; }
    
    [MaxLength(20)]
    public string? CNIC { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;
    
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? Province { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(10)]
    public string? PostalCode { get; set; }
    
    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }
    
    [MaxLength(15)]
    public string? EmergencyContactPhone { get; set; }
    
    [MaxLength(100)]
    public string? PhotoPath { get; set; }
    
    public DateTime HireDate { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Inactive, Terminated
    
    [MaxLength(100)]
    public string? Qualification { get; set; }
    
    [MaxLength(100)]
    public string? Specialization { get; set; }
    
    public decimal? Salary { get; set; }
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    // User Link (for authentication)
    public string? UserId { get; set; }
    
    // Computed Properties
    public string FullName => $"{FirstName} {LastName}";
    
    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
    
    public virtual ICollection<Batch> PrimaryBatches { get; set; } = new List<Batch>();
    public virtual ICollection<Batch> SecondaryBatches { get; set; } = new List<Batch>();
    public virtual ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
}