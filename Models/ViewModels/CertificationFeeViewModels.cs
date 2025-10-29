using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class CertificationFeeCollectionViewModel
{
    public int StudentId { get; set; }
    
    [Display(Name = "Student Name")]
    public string StudentName { get; set; } = string.Empty;
    
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;
    
    [Display(Name = "Trade")]
    public string TradeName { get; set; } = string.Empty;
    
    [Display(Name = "Session")]
    public string SessionName { get; set; } = string.Empty;
    
    [Display(Name = "Previous Qualification")]
    public string PreviousQualification { get; set; } = string.Empty;
    
    [Display(Name = "Qualification Level")]
    public string QualificationLevel { get; set; } = string.Empty;
    
    [Display(Name = "Certification Fee")]
    public decimal CertificationFee { get; set; }
    
    [Display(Name = "Amount Paid")]
    public decimal CertificationFeePaid { get; set; }
    
    [Display(Name = "Remaining Amount")]
    public decimal RemainingAmount { get; set; }
    
    [Required(ErrorMessage = "Amount is required")]
    [Display(Name = "Payment Amount")]
    [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999,999.99")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "Payment date is required")]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Payment method is required")]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Cash";
    
    [Display(Name = "Remarks")]
    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
}

public class ApplyCertificationFeeViewModel
{
    public int StudentId { get; set; }
    
    [Display(Name = "Student Name")]
    public string StudentName { get; set; } = string.Empty;
    
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;
    
    [Display(Name = "Previous Qualification")]
    public string PreviousQualification { get; set; } = string.Empty;
    
    [Display(Name = "Qualification Level")]
    public string QualificationLevel { get; set; } = string.Empty;
    
    [Display(Name = "Certification Fee Amount")]
    public decimal CertificationFeeAmount { get; set; }
    
    [Display(Name = "Already Applied")]
    public bool IsAlreadyApplied { get; set; }
    
    [Display(Name = "Applied Date")]
    public DateTime? AppliedDate { get; set; }
}

public class CertificationFeeListViewModel
{
    public List<CertificationFeeStudentViewModel> Students { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty; // Applied, NotApplied, Paid, Pending
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
}

public class CertificationFeeStudentViewModel
{
    public int StudentId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string PreviousQualification { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public bool IsCertificationFeeApplicable { get; set; }
    public decimal CertificationFee { get; set; }
    public decimal CertificationFeePaid { get; set; }
    public decimal CertificationFeeRemaining { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? CertificationFeeAppliedDate { get; set; }
    public string? CertificationStatus { get; set; }
    public DateTime? CertificationReadyDate { get; set; }
    public DateTime? CertificationHandedDate { get; set; }
}

public class UpdateCertificationStatusViewModel
{
    public int StudentId { get; set; }
    
    [Display(Name = "Student Name")]
    public string StudentName { get; set; } = string.Empty;
    
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;
    
    [Display(Name = "Trade")]
    public string TradeName { get; set; } = string.Empty;
    
    [Display(Name = "Previous Qualification")]
    public string PreviousQualification { get; set; } = string.Empty;
    
    [Display(Name = "Certification Fee Status")]
    public string FeePaymentStatus { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Certification status is required")]
    [Display(Name = "Certification Status")]
    public string CertificationStatus { get; set; } = string.Empty;
    
    [Display(Name = "Certificate Ready Date")]
    public DateTime? CertificationReadyDate { get; set; }
    
    [Display(Name = "Certificate Handed Date")]
    public DateTime? CertificationHandedDate { get; set; }
    
    [Display(Name = "Remarks")]
    [MaxLength(500)]
    public string? CertificationRemarks { get; set; }
    
    [Display(Name = "Current Status")]
    public string? CurrentStatus { get; set; }
    
    [Display(Name = "Current Ready Date")]
    public DateTime? CurrentReadyDate { get; set; }
    
    [Display(Name = "Current Handed Date")]
    public DateTime? CurrentHandedDate { get; set; }
}
