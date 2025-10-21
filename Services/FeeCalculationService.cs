using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Services;

public interface IFeeCalculationService
{
    Task<decimal> CalculateStudentFeeAsync(int studentId, int tradeId, int sessionId);
    Task<FeeCalculationResult> CalculateDetailedFeeAsync(int studentId, int tradeId, int sessionId);
    Task<decimal> GetBaseFeeForTradeAsync(int tradeId, int sessionId, string studentGender);
}

public class FeeCalculationService : IFeeCalculationService
{
    private readonly ApplicationDbContext _context;
    
    public FeeCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<decimal> CalculateStudentFeeAsync(int studentId, int tradeId, int sessionId)
    {
        var result = await CalculateDetailedFeeAsync(studentId, tradeId, sessionId);
        return result.FinalAmount;
    }
    
    public async Task<FeeCalculationResult> CalculateDetailedFeeAsync(int studentId, int tradeId, int sessionId)
    {
        // Get student details
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId);
            
        if (student == null)
            throw new ArgumentException("Student not found", nameof(studentId));
        
        // Get base fee structure
        var feeStructure = await _context.FeeStructures
            .Where(fs => fs.TradeId == tradeId && 
                        fs.SessionId == sessionId && 
                        fs.IsActive &&
                        fs.EffectiveFrom <= DateTime.Now &&
                        (fs.EffectiveTo == null || fs.EffectiveTo >= DateTime.Now))
            .OrderByDescending(fs => fs.EffectiveFrom)
            .FirstOrDefaultAsync();
            
        if (feeStructure == null)
            throw new InvalidOperationException($"No active fee structure found for Trade {tradeId} and Session {sessionId}");
        
        // Calculate base fee based on gender
        var baseFee = await GetBaseFeeForTradeAsync(tradeId, sessionId, student.Gender);
        
        // Get any fee adjustments for this student
        var adjustments = await _context.StudentFeeAdjustments
            .Where(sfa => sfa.StudentId == studentId && 
                         sfa.FeeStructureId == feeStructure.Id &&
                         sfa.IsActive &&
                         sfa.EffectiveFrom <= DateTime.Now &&
                         (sfa.EffectiveTo == null || sfa.EffectiveTo >= DateTime.Now))
            .ToListAsync();
        
        var result = new FeeCalculationResult
        {
            StudentId = studentId,
            TradeId = tradeId,
            SessionId = sessionId,
            BaseFee = baseFee,
            FeeStructure = feeStructure
        };
        
        // Apply adjustments
        decimal totalAdjustment = 0;
        var adjustmentDetails = new List<AdjustmentDetail>();
        
        foreach (var adjustment in adjustments)
        {
            decimal adjustmentAmount = 0;
            
            if (adjustment.IsFree || adjustment.AdjustmentType == AdjustmentType.CompleteWaiver)
            {
                adjustmentAmount = -baseFee; // Full waiver
                result.IsFree = true;
            }
            else if (adjustment.AdjustmentType == AdjustmentType.FixedAmount)
            {
                adjustmentAmount = -(adjustment.AdjustmentAmount ?? 0);
            }
            else if (adjustment.AdjustmentType == AdjustmentType.Percentage && adjustment.AdjustmentPercentage.HasValue)
            {
                adjustmentAmount = -(baseFee * adjustment.AdjustmentPercentage.Value / 100);
            }
            
            totalAdjustment += adjustmentAmount;
            
            adjustmentDetails.Add(new AdjustmentDetail
            {
                ConcessionType = adjustment.ConcessionType,
                AdjustmentAmount = adjustmentAmount,
                Reason = adjustment.Reason
            });
        }
        
        result.TotalAdjustment = totalAdjustment;
        result.FinalAmount = Math.Max(0, baseFee + totalAdjustment); // Ensure non-negative
        result.AdjustmentDetails = adjustmentDetails;
        
        return result;
    }
    
    public async Task<decimal> GetBaseFeeForTradeAsync(int tradeId, int sessionId, string studentGender)
    {
        var feeStructure = await _context.FeeStructures
            .Where(fs => fs.TradeId == tradeId && 
                        fs.SessionId == sessionId && 
                        fs.IsActive &&
                        fs.EffectiveFrom <= DateTime.Now &&
                        (fs.EffectiveTo == null || fs.EffectiveTo >= DateTime.Now))
            .OrderByDescending(fs => fs.EffectiveFrom)
            .FirstOrDefaultAsync();
            
        if (feeStructure == null)
            return 0;
        
        // Check if there's a standard fee (same for all genders)
        if (feeStructure.StandardFee.HasValue && feeStructure.StandardFee.Value > 0)
            return feeStructure.StandardFee.Value;
        
        // Use gender-specific fees
        return studentGender?.ToLower() switch
        {
            "male" => feeStructure.MaleFee,
            "female" => feeStructure.FemaleFee,
            "transgender" => feeStructure.TransgenderFee,
            _ => feeStructure.FemaleFee // Default to female fee for unknown genders
        };
    }
}

public class FeeCalculationResult
{
    public int StudentId { get; set; }
    public int TradeId { get; set; }
    public int SessionId { get; set; }
    public decimal BaseFee { get; set; }
    public decimal TotalAdjustment { get; set; }
    public decimal FinalAmount { get; set; }
    public bool IsFree { get; set; }
    public FeeStructure? FeeStructure { get; set; }
    public List<AdjustmentDetail> AdjustmentDetails { get; set; } = new();
}

public class AdjustmentDetail
{
    public string? ConcessionType { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string? Reason { get; set; }
}