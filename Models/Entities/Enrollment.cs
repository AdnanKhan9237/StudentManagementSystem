using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

public class Enrollment : BaseEntity
{
    // Foreign Keys
    public int StudentId { get; set; }
    public int SessionId { get; set; }
    public int TradeId { get; set; }
    public int? BatchId { get; set; }
    public int? TimingId { get; set; }

    [Required]
    [MaxLength(20)]
    public string RegNo { get; set; } = string.Empty; // Registration number scoped per session

    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Graduated, Dropped, Suspended

    [MaxLength(500)]
    public string? Remarks { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(SessionId))]
    public virtual Session Session { get; set; } = null!;

    [ForeignKey(nameof(TradeId))]
    public virtual Trade Trade { get; set; } = null!;

    [ForeignKey(nameof(BatchId))]
    public virtual Batch? Batch { get; set; }

    [ForeignKey(nameof(TimingId))]
    public virtual Timing? Timing { get; set; }

    // Navigation: attendance records under this enrollment
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
