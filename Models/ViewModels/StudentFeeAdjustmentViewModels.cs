using System.ComponentModel.DataAnnotations;
using StudentManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.Models.ViewModels;

public class StudentFeeAdjustmentCreateViewModel
{
    [Required(ErrorMessage = "Student is required")]
    [Display(Name = "Student")]
    public int StudentId { get; set; }
    
    [Required(ErrorMessage = "Fee Structure is required")]
    [Display(Name = "Fee Structure")]
    public int FeeStructureId { get; set; }
    
    [Required(ErrorMessage = "Adjustment type is required")]
    [Display(Name = "Adjustment Type")]
    public AdjustmentType AdjustmentType { get; set; }
    
    [Display(Name = "Adjustment Amount")]
    [Range(0, 999999.99, ErrorMessage = "Adjustment amount must be between 0 and 999,999.99")]
    public decimal? AdjustmentAmount { get; set; }
    
    [Display(Name = "Adjustment Percentage")]
    [Range(0, 100, ErrorMessage = "Adjustment percentage must be between 0 and 100")]
    public decimal? AdjustmentPercentage { get; set; }
    
    [Display(Name = "Is Free (Complete Waiver)")]
    public bool IsFree { get; set; }
    
    [Display(Name = "Concession Type")]
    [MaxLength(100, ErrorMessage = "Concession type cannot exceed 100 characters")]
    public string? ConcessionType { get; set; }
    
    [Required(ErrorMessage = "Reason is required")]
    [Display(Name = "Reason")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
    
    [Display(Name = "Approved By")]
    [MaxLength(200, ErrorMessage = "Approved by cannot exceed 200 characters")]
    public string? ApprovedBy { get; set; }
    
    [Required(ErrorMessage = "Approval date is required")]
    [Display(Name = "Approved Date")]
    public DateTime ApprovedDate { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "Effective from date is required")]
    [Display(Name = "Effective From")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Now;
    
    [Display(Name = "Effective To")]
    public DateTime? EffectiveTo { get; set; }
    
    [Display(Name = "Supporting Documents")]
    [MaxLength(200, ErrorMessage = "Supporting documents cannot exceed 200 characters")]
    public string? SupportingDocuments { get; set; }
    
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
    
    [Display(Name = "Remarks")]
    [MaxLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
    public string? Remarks { get; set; }
    
    // Dropdowns and additional data
    public SelectList? Students { get; set; }
    public SelectList? FeeStructures { get; set; }
    public SelectList? ConcessionTypes { get; set; }
}

public class StudentFeeAdjustmentEditViewModel : StudentFeeAdjustmentCreateViewModel
{
    public int Id { get; set; }
}

public class StudentFeeAdjustmentDetailsViewModel
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentRegistrationNumber { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public AdjustmentType AdjustmentType { get; set; }
    public decimal? AdjustmentAmount { get; set; }
    public decimal? AdjustmentPercentage { get; set; }
    public bool IsFree { get; set; }
    public string? ConcessionType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime ApprovedDate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? SupportingDocuments { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public decimal BaseFee { get; set; }
    public decimal CalculatedAdjustment { get; set; }
    public decimal FinalFee { get; set; }
    
    public string AdjustmentTypeDisplay
    {
        get
        {
            return AdjustmentType switch
            {
                AdjustmentType.FixedAmount => "Fixed Amount",
                AdjustmentType.Percentage => "Percentage",
                AdjustmentType.CompleteWaiver => "Complete Waiver",
                _ => "Unknown"
            };
        }
    }
    
    public string AdjustmentDisplay
    {
        get
        {
            if (IsFree || AdjustmentType == AdjustmentType.CompleteWaiver)
                return "Complete Fee Waiver";
            
            if (AdjustmentType == AdjustmentType.FixedAmount && AdjustmentAmount.HasValue)
                return $"Rs. {AdjustmentAmount:N2} reduction";
            
            if (AdjustmentType == AdjustmentType.Percentage && AdjustmentPercentage.HasValue)
                return $"{AdjustmentPercentage}% reduction";
            
            return "No adjustment";
        }
    }
}

public class StudentFeeAdjustmentListViewModel
{
    public List<StudentFeeAdjustmentListItemViewModel> Adjustments { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public int? SelectedStudentId { get; set; }
    public int? SelectedTradeId { get; set; }
    public int? SelectedSessionId { get; set; }
    public AdjustmentType? SelectedAdjustmentType { get; set; }
    public string? SelectedConcessionType { get; set; }
    public SelectList? Students { get; set; }
    public SelectList? Trades { get; set; }
    public SelectList? Sessions { get; set; }
    public SelectList? AdjustmentTypes { get; set; }
    public SelectList? ConcessionTypes { get; set; }
}

public class StudentFeeAdjustmentListItemViewModel
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentRegistrationNumber { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public AdjustmentType AdjustmentType { get; set; }
    public decimal? AdjustmentAmount { get; set; }
    public decimal? AdjustmentPercentage { get; set; }
    public bool IsFree { get; set; }
    public string? ConcessionType { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    
    public string AdjustmentTypeDisplay
    {
        get
        {
            return AdjustmentType switch
            {
                AdjustmentType.FixedAmount => "Fixed",
                AdjustmentType.Percentage => "Percentage",
                AdjustmentType.CompleteWaiver => "Waiver",
                _ => "Unknown"
            };
        }
    }
    
    public string AdjustmentDisplay
    {
        get
        {
            if (IsFree || AdjustmentType == AdjustmentType.CompleteWaiver)
                return "Free";
            
            if (AdjustmentType == AdjustmentType.FixedAmount && AdjustmentAmount.HasValue)
                return $"Rs. {AdjustmentAmount:N2}";
            
            if (AdjustmentType == AdjustmentType.Percentage && AdjustmentPercentage.HasValue)
                return $"{AdjustmentPercentage}%";
            
            return "-";
        }
    }
    
    public string EffectivePeriod
    {
        get
        {
            if (EffectiveTo.HasValue)
                return $"{EffectiveFrom:MMM dd, yyyy} - {EffectiveTo:MMM dd, yyyy}";
            return $"{EffectiveFrom:MMM dd, yyyy} - Ongoing";
        }
    }
}