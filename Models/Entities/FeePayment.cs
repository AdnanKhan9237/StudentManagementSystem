using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models.Entities;

[Table("FeePayments")]
public class FeePayment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Student))]
    public int StudentId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(100)]
    public string ModifiedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual Student Student { get; set; } = null!;
}