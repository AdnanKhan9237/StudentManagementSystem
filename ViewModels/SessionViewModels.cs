using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class SessionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Session name is required")]
    [StringLength(100, ErrorMessage = "Session name cannot exceed 100 characters")]
    [Display(Name = "Session Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Session code is required")]
    [StringLength(10, ErrorMessage = "Session code cannot exceed 10 characters")]
    [Display(Name = "Session Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Display(Name = "Current Session")]
    public bool IsCurrentSession { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    [Required(ErrorMessage = "Session type is required")]
    [Display(Name = "Session Type")]
    public string SessionType { get; set; } = "Regular";
    
    [Required(ErrorMessage = "Duration is required")]
    [Display(Name = "Duration (Months)")]
    [Range(1, 60, ErrorMessage = "Duration must be between 1 and 60 months")]
    public int DurationMonths { get; set; } = 6;
    
    [Display(Name = "Registration Start Date")]
    public DateTime? RegistrationStartDate { get; set; }
    
    [Display(Name = "Registration End Date")]
    public DateTime? RegistrationEndDate { get; set; }
    
    [Display(Name = "Description")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    // Display properties
    public int StudentCount { get; set; }
    public int TotalStudents { get; set; }
    public int TotalBatches { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string Duration => $"{StartDate:MMM yyyy} - {EndDate:MMM yyyy} ({DurationMonths} months)";
    public string Status => IsCurrentSession ? "Current" : IsActive ? "Active" : "Inactive";
    public string SessionTypeDisplay => SessionType switch
    {
        "Short-Term" => "Short-Term (3 months)",
        "Long-Term" => "Long-Term (6+ months)",
        _ => "Regular (6 months)"
    };
    public List<BatchSummaryViewModel> Batches { get; set; } = new();
}

public class SessionListViewModel
{
    public List<SessionViewModel> Sessions { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool? ActiveFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public string[] Statuses { get; set; } = new[] { "Active", "Upcoming", "Completed", "Archived" };
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class BatchSummaryViewModel
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StudentCount { get; set; }
}

public class StudentSummaryViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
}