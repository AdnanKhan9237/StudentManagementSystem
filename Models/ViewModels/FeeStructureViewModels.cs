using System.ComponentModel.DataAnnotations;
using StudentManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.Models.ViewModels;

public class FeeStructureCreateViewModel
{
    [Required(ErrorMessage = "Trade is required")]
    [Display(Name = "Trade")]
    public int TradeId { get; set; }
    
    [Required(ErrorMessage = "Session is required")]
    [Display(Name = "Session")]
    public int SessionId { get; set; }
    
    [Display(Name = "Male Fee")]
    [Range(0, 999999.99, ErrorMessage = "Male fee must be between 0 and 999,999.99")]
    public decimal MaleFee { get; set; }
    
    [Display(Name = "Female Fee")]
    [Range(0, 999999.99, ErrorMessage = "Female fee must be between 0 and 999,999.99")]
    public decimal FemaleFee { get; set; }
    
    [Display(Name = "Transgender Fee")]
    [Range(0, 999999.99, ErrorMessage = "Transgender fee must be between 0 and 999,999.99")]
    public decimal TransgenderFee { get; set; }
    
    [Display(Name = "Standard Fee (Same for All)")]
    [Range(0, 999999.99, ErrorMessage = "Standard fee must be between 0 and 999,999.99")]
    public decimal? StandardFee { get; set; }
    
    [Display(Name = "Tuition Fee")]
    [Range(0, 999999.99, ErrorMessage = "Tuition fee must be between 0 and 999,999.99")]
    public decimal? TuitionFee { get; set; }
    
    [Display(Name = "Admission Fee")]
    [Range(0, 999999.99, ErrorMessage = "Admission fee must be between 0 and 999,999.99")]
    public decimal? AdmissionFee { get; set; }
    
    [Display(Name = "Security Fee")]
    [Range(0, 999999.99, ErrorMessage = "Security fee must be between 0 and 999,999.99")]
    public decimal? SecurityFee { get; set; }
    
    [Display(Name = "Exam Fee")]
    [Range(0, 999999.99, ErrorMessage = "Exam fee must be between 0 and 999,999.99")]
    public decimal? ExamFee { get; set; }
    
    [Display(Name = "Certificate Fee")]
    [Range(0, 999999.99, ErrorMessage = "Certificate fee must be between 0 and 999,999.99")]
    public decimal? CertificateFee { get; set; }
    
    [Display(Name = "Lab Fee")]
    [Range(0, 999999.99, ErrorMessage = "Lab fee must be between 0 and 999,999.99")]
    public decimal? LabFee { get; set; }
    
    [Display(Name = "Material Fee")]
    [Range(0, 999999.99, ErrorMessage = "Material fee must be between 0 and 999,999.99")]
    public decimal? MaterialFee { get; set; }
    
    [Required(ErrorMessage = "Effective from date is required")]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Now;
    
    [Display(Name = "Effective To")]
    public DateTime? EffectiveTo { get; set; }
    
    [Display(Name = "Description")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [Display(Name = "Fee Category")]
    [MaxLength(200, ErrorMessage = "Fee category cannot exceed 200 characters")]
    public string? FeeCategory { get; set; }
    
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    
    // Dropdowns
    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
}

public class FeeStructureEditViewModel : FeeStructureCreateViewModel
{
    public int Id { get; set; }
}

public class FeeStructureDetailsViewModel
{
    public int Id { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public decimal MaleFee { get; set; }
    public decimal FemaleFee { get; set; }
    public decimal TransgenderFee { get; set; }
    public decimal? StandardFee { get; set; }
    public decimal? TuitionFee { get; set; }
    public decimal? AdmissionFee { get; set; }
    public decimal? SecurityFee { get; set; }
    public decimal? ExamFee { get; set; }
    public decimal? CertificateFee { get; set; }
    public decimal? LabFee { get; set; }
    public decimal? MaterialFee { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public string? FeeCategory { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FeeStructureListViewModel
{
    public List<FeeStructureListItemViewModel> FeeStructures { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public int? SelectedTradeId { get; set; }
    public int? SelectedSessionId { get; set; }
    public string? SelectedCategory { get; set; }
    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
}

public class FeeStructureListItemViewModel
{
    public int Id { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public decimal MaleFee { get; set; }
    public decimal FemaleFee { get; set; }
    public decimal TransgenderFee { get; set; }
    public decimal? StandardFee { get; set; }
    public string? FeeCategory { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    
    public string EffectivePeriod 
    { 
        get 
        {
            if (EffectiveTo.HasValue)
                return $"{EffectiveFrom:MMM dd, yyyy} - {EffectiveTo:MMM dd, yyyy}";
            return $"{EffectiveFrom:MMM dd, yyyy} - Ongoing";
        }
    }
    
    public string FeeDisplay
    {
        get
        {
            if (StandardFee.HasValue && StandardFee.Value > 0)
                return $"Rs. {StandardFee:N2}";
            
            return $"M: Rs. {MaleFee:N2}, F: Rs. {FemaleFee:N2}, T: Rs. {TransgenderFee:N2}";
        }
    }
}