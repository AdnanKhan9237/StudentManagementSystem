using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

/// <summary>
/// Junction table for Batch-Timing many-to-many relationship
/// Allows batches to have multiple timings with separate seat limits
/// </summary>
public class BatchTiming : BaseEntity
{
    [Required]
    public int BatchId { get; set; }
    
    [Required]
    public int TimingId { get; set; }
    
    /// <summary>
    /// Maximum students allowed for this specific batch-timing combination
    /// </summary>
    [Required]
    [Range(1, 50)]
    public int MaxStudents { get; set; } = 50;
    
    // Navigation Properties
    [ForeignKey(nameof(BatchId))]
    public virtual Batch Batch { get; set; } = null!;
    
    [ForeignKey(nameof(TimingId))]
    public virtual Timing Timing { get; set; } = null!;
    
    // For tracking current enrollment per timing
    [NotMapped]
    public int CurrentStudents { get; set; }
    
    [NotMapped]
    public int AvailableSeats => MaxStudents - CurrentStudents;
}
