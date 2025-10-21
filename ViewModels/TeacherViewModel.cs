using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.ViewModels;

public class TeacherViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Teacher code is required")]
    [Display(Name = "Teacher Code")]
    public string TeacherCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Father Name")]
    public string? FatherName { get; set; }

    [Display(Name = "CNIC")]
    [StringLength(15)]
    public string? CNIC { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [Phone]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "Province")]
    public string? Province { get; set; }

    [Display(Name = "Country")]
    public string? Country { get; set; }

    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Emergency Contact Name")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "Emergency Contact Phone")]
    [Phone]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Photo { get; set; }

    public string? PhotoPath { get; set; }

    [Required(ErrorMessage = "Hire date is required")]
    [Display(Name = "Hire Date")]
    [DataType(DataType.Date)]
    public DateTime HireDate { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Active";

    [Display(Name = "Qualification")]
    public string? Qualification { get; set; }

    [Display(Name = "Specialization")]
    public string? Specialization { get; set; }

    [Display(Name = "Salary")]
    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    public decimal? Salary { get; set; }

    [Display(Name = "Remarks")]
    [StringLength(500)]
    public string? Remarks { get; set; }

    // For password management
    [Display(Name = "Password")]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }

    // Display properties
    public string FullName => $"{FirstName} {LastName}";
    public int PrimaryBatchCount { get; set; }
    public int SecondaryBatchCount { get; set; }
    public List<string> AssignedBatches { get; set; } = new();

    // Audit Properties
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    // Dropdowns
    public SelectList? StatusOptions { get; set; }
}

public class TeacherListViewModel
{
    public List<TeacherViewModel> Teachers { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

    public string[] Statuses { get; set; } = new[] { "Active", "Inactive", "Terminated" };
    public SelectList? StatusOptions { get; set; }
}

public class TeacherDashboardViewModel
{
    public TeacherViewModel Teacher { get; set; } = new();
    public List<TeacherBatchSummaryViewModel> PrimaryBatches { get; set; } = new();
    public List<TeacherBatchSummaryViewModel> SecondaryBatches { get; set; } = new();
    public int TotalStudents { get; set; }
    public List<AttendanceSummaryViewModel> RecentAttendance { get; set; } = new();
}

public class TeacherBatchSummaryViewModel
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AttendanceSummaryViewModel
{
    public DateTime Date { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int TotalCount { get; set; }
    public double AttendancePercentage => TotalCount > 0 ? (double)PresentCount / TotalCount * 100 : 0;
}