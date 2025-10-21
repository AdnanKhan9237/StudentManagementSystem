using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class FeeTransaction : BaseEntity
{
    // Foreign Keys
    public int StudentId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string TransactionNumber { get; set; } = string.Empty;
    
    public DateTime TransactionDate { get; set; }
    
    public decimal Amount { get; set; }
    
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty; // Payment, Refund, Adjustment
    
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Bank, Online, Cheque
    
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; } // Bank reference, cheque number, etc.
    
    [MaxLength(100)]
    public string? BankName { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
    
    public string? ProcessedBy { get; set; }
    
    [MaxLength(100)]
    public string? ReceiptPath { get; set; } // Path to generated receipt
    
    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;
    
    [ForeignKey(nameof(ProcessedBy))]
    public virtual ApplicationUser? ProcessedByUser { get; set; }
}