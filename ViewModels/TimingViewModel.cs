using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class TimingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [Required(ErrorMessage = "Shift is required")]
    [Display(Name = "Shift")]
    [StringLength(20)]
    public string Shift { get; set; } = string.Empty;

    [Display(Name = "Type")]
    [StringLength(50)]
    public string? Type { get; set; }

    [Display(Name = "Description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Additional properties for display
    public int BatchCount { get; set; }
    public List<BatchSummaryViewModel> AssignedBatches { get; set; } = new();

    // Audit Properties
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    // Calculated properties
    public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public string DisplayName => $"{Name} ({TimeRange})";
    public string Duration
    {
        get
        {
            var duration = EndTime - StartTime;
            if (duration.TotalDays < 0) // Handle times that cross midnight
            {
                duration = duration.Add(TimeSpan.FromDays(1));
            }
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
}

public class TimingListViewModel
{
    public List<TimingViewModel> Timings { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string TypeFilter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public List<string?> TimingTypes { get; set; } = new();
}
