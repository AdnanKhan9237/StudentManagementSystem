using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Student : BaseEntity
{
    [Required]
    [MaxLength(20)]
    public string RegistrationNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string StudentCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(100)]
    public string? FatherName { get; set; }
    
    [MaxLength(20)]
    public string? CNIC { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? BloodGroup { get; set; }
    
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
    
    public DateTime AdmissionDate { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Graduated, Dropped, Suspended
    
    // Foreign Keys
    public int TradeId { get; set; }
    public int SessionId { get; set; }
    public int? BatchId { get; set; }
    public int? TimingId { get; set; }
    
    // User Link (for role-based access)
    public string? UserId { get; set; }
    
    // Academic Information
    [MaxLength(50)]
    public string? PreviousQualification { get; set; }
    
    [MaxLength(100)]
    public string? PreviousInstitute { get; set; }
    
    public decimal? PreviousMarks { get; set; }
    
    // Fee Information
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => TotalFee - PaidAmount;
    
    // Certification Fee
    public decimal CertificationFee { get; set; }
    public decimal CertificationFeePaid { get; set; }
    public decimal CertificationFeeRemaining => CertificationFee - CertificationFeePaid;
    public bool IsCertificationFeeApplicable { get; set; } = false;
    public DateTime? CertificationFeeAppliedDate { get; set; }
    
    // Physical Certification Status
    [MaxLength(50)]
    public string? CertificationStatus { get; set; } // FeeNotPaid, FeePaid, InProgress, Ready, Handed
    public DateTime? CertificationReadyDate { get; set; }
    public DateTime? CertificationHandedDate { get; set; }
    
    [MaxLength(500)]
    public string? CertificationRemarks { get; set; }
    
    [MaxLength(500)]
    public string? Remarks { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(TradeId))]
    public virtual Trade Trade { get; set; } = null!;
    
    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;
    
    [ForeignKey(nameof(BatchId))]
    public virtual Batch? Batch { get; set; }
    
    [ForeignKey(nameof(TimingId))]
    public virtual Timing? Timing { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
    
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    public virtual ICollection<FeeTransaction> FeeTransactions { get; set; } = new List<FeeTransaction>();
    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    // New: support multiple enrollments per student
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
