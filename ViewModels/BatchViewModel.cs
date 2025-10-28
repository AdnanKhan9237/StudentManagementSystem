using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.ViewModels;

public class BatchViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Batch code is required")]
    [Display(Name = "Batch Code")]
    public string BatchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Batch name is required")]
    [Display(Name = "Batch Name")]
    public string BatchName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trade is required")]
    [Display(Name = "Trade")]
    public int TradeId { get; set; }

    [Required(ErrorMessage = "Session is required")]
    [Display(Name = "Session")]
    public int SessionId { get; set; }

    [Display(Name = "Timing")]
    public int? TimingId { get; set; }
    
    [Display(Name = "Timings")]
    public List<int> SelectedTimingIds { get; set; } = new();
    
    // For displaying batch timings with max students
    public List<BatchTimingViewModel> BatchTimings { get; set; } = new();

    [Display(Name = "Room")]
    public int? RoomId { get; set; }

    [Display(Name = "Primary Instructor")]
    public int? PrimaryInstructorId { get; set; }
    
    [Display(Name = "Secondary Instructor")]
    public int? SecondaryInstructorId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Display(Name = "Maximum Students")]
    [Range(0, 200, ErrorMessage = "Maximum students must be between 0 and 200")]
    public int MaxStudents { get; set; }

    [Display(Name = "Current Enrollment")]
    public int CurrentEnrollment { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Active";

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties for Display
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string TimingDescription { get; set; } = string.Empty;
    public string TimingName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string PrimaryInstructorName { get; set; } = string.Empty;
    public string SecondaryInstructorName { get; set; } = string.Empty;
    
    // Additional properties for controllers
    public int CurrentStudents { get; set; }
    public string? Description { get; set; }
    public List<StudentSummaryViewModel> EnrolledStudents { get; set; } = new();

    // Audit Properties
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    // Select Lists for Dropdowns
    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
    public SelectList? Timings { get; set; }
    public SelectList? Rooms { get; set; }
    public SelectList? PrimaryInstructors { get; set; }
    public SelectList? SecondaryInstructors { get; set; }
    public SelectList? StatusOptions { get; set; }

    // Calculated Properties
    public int AvailableSlots => MaxStudents - CurrentEnrollment;
    public bool IsFull => CurrentEnrollment >= MaxStudents;
    public double FillPercentage => MaxStudents > 0 ? (double)CurrentEnrollment / MaxStudents * 100 : 0;
}

public class BatchListViewModel
{
    public List<BatchViewModel> Batches { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public int? TradeId { get; set; }
    public int? SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

    public List<dynamic> Trades { get; set; } = new();
    public List<dynamic> Sessions { get; set; } = new();
    public string[] Statuses { get; set; } = Array.Empty<string>();
    
    // Additional properties for compatibility
    public int? TradeFilter { get; set; }
    public int? SessionFilter { get; set; }
    public string StatusFilter { get; set; } = string.Empty;
    public SelectList? StatusOptions { get; set; }
}

public class BatchTimingViewModel
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int TimingId { get; set; }
    public string TimingName { get; set; } = string.Empty;
    public string TimingDescription { get; set; } = string.Empty;
    public int MaxStudents { get; set; }
    public int CurrentStudents { get; set; }
    public int AvailableSeats => MaxStudents - CurrentStudents;
}
