using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class TradeViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "English name is required")]
    [Display(Name = "Name (English)")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string NameEnglish { get; set; } = string.Empty;

    [Required(ErrorMessage = "Urdu name is required")]
    [Display(Name = "Name (Urdu)")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string NameUrdu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trade code is required")]
    [Display(Name = "Trade Code")]
    [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Description (English)")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? DescriptionEnglish { get; set; }

    [Display(Name = "Description (Urdu)")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? DescriptionUrdu { get; set; }

    [Required(ErrorMessage = "Duration is required")]
    [Display(Name = "Duration (Months)")]
    [Range(1, 60, ErrorMessage = "Duration must be between 1 and 60 months")]
    public int Duration { get; set; }

    [Display(Name = "Fee Type")]
    public string FeeType { get; set; } = "Standard"; // Standard, Gender-Based

    [Display(Name = "Standard Fee (₨)")]
    [Range(0, double.MaxValue, ErrorMessage = "Fee must be positive")]
    public decimal? StandardFee { get; set; }

    [Display(Name = "Male Fee (₨)")]
    [Range(0, double.MaxValue, ErrorMessage = "Male fee must be positive")]
    public decimal? MaleFee { get; set; }

    [Display(Name = "Female Fee (₨)")]
    [Range(0, double.MaxValue, ErrorMessage = "Female fee must be positive")]
    public decimal? FemaleFee { get; set; }

    [Display(Name = "Transgender Fee (₨)")]
    [Range(0, double.MaxValue, ErrorMessage = "Transgender fee must be positive")]
    public decimal? TransgenderFee { get; set; }

    // Keep for backward compatibility
    public decimal TotalFee 
    { 
        get 
        {
            if (FeeType == "Gender-Based" && MaleFee.HasValue)
                return MaleFee.Value; // Default to male fee for compatibility
            return StandardFee ?? 0;
        }
        set 
        {
            if (FeeType == "Standard")
                StandardFee = value;
        }
    }

    [Required(ErrorMessage = "Maximum students is required")]
    [Display(Name = "Maximum Students")]
    [Range(1, 1000, ErrorMessage = "Maximum students must be between 1 and 1000")]
    public int MaxStudents { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Modified Date")]
    public DateTime? ModifiedDate { get; set; }

    [Display(Name = "Modified By")]
    public string? ModifiedBy { get; set; }

    // Additional properties for display
    public int CurrentStudents { get; set; }
    public int AvailableSlots => MaxStudents - CurrentStudents;
}

public class TradeListViewModel
{
    public List<TradeViewModel> Trades { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public bool? ActiveFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
}