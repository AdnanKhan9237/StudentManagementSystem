using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.ViewModels;

public class StudentViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Registration Number is required")]
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First Name is required")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Father Name")]
    public string? FatherName { get; set; }

    [Display(Name = "CNIC")]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC format should be 12345-1234567-1")]
    public string? CNIC { get; set; }

    [Required(ErrorMessage = "Date of Birth is required")]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    public string Gender { get; set; } = string.Empty;

    [Display(Name = "Blood Group")]
    public string? BloodGroup { get; set; }

    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Emergency Contact Name")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Emergency Contact Phone")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Admission Date")]
    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    public string Status { get; set; } = "Active";

    [Required(ErrorMessage = "Trade is required")]
    [Display(Name = "Trade")]
    public int TradeId { get; set; }

    [Required(ErrorMessage = "Session is required")]
    [Display(Name = "Session")]
    public int SessionId { get; set; }

    [Display(Name = "Batch")]
    public int? BatchId { get; set; }

    [Display(Name = "Previous Qualification")]
    public string? PreviousQualification { get; set; }

    [Display(Name = "Previous Institute")]
    public string? PreviousInstitute { get; set; }

    [Display(Name = "Previous Marks")]
    [Range(0, 100, ErrorMessage = "Marks should be between 0 and 100")]
    public decimal? PreviousMarks { get; set; }

    [Required(ErrorMessage = "Total Fee is required")]
    [Display(Name = "Total Fee")]
    [Range(0, double.MaxValue, ErrorMessage = "Fee must be positive")]
    public decimal TotalFee { get; set; }

    [Display(Name = "Paid Amount")]
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal PaidAmount { get; set; }

    [Display(Name = "Remaining Amount")]
    public decimal RemainingAmount => TotalFee - PaidAmount;

    public string? Remarks { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? PhotoFile { get; set; }

    public string? PhotoPath { get; set; }

    // Navigation Properties for Display
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;

    // Select Lists for Dropdowns
    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
    public SelectList? Batches { get; set; }
    public SelectList? GenderOptions { get; set; }
    public SelectList? StatusOptions { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public int Age => DateTime.Now.Year - DateOfBirth.Year;
}

public class StudentListViewModel
{
    public List<StudentViewModel> Students { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public int? TradeFilter { get; set; }
    public int? SessionFilter { get; set; }
    public string StatusFilter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
    public SelectList? StatusOptions { get; set; }
}