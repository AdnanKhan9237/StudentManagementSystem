using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class FeeStatusViewModel
{
    public int StudentId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class FeeManagementListViewModel
{
    public List<FeeStatusViewModel> Students { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class StudentFeeDetailsViewModel
{
    public int StudentId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public List<PaymentHistoryViewModel> PaymentHistory { get; set; } = new();
}

public class PaymentHistoryViewModel
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public class FeeCollectionViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    [Required(ErrorMessage = "Payment amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
    public string Remarks { get; set; } = string.Empty;
}

public class FeeReportsViewModel
{
    public int TotalStudents { get; set; }
    public decimal TotalFeeAmount { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalPending { get; set; }
    public double CollectionRate { get; set; }
    
    public int PaidStudents { get; set; }
    public int PartialStudents { get; set; }
    public int UnpaidStudents { get; set; }
    
    public List<TradeWiseFeeViewModel> TradeWiseData { get; set; } = new();
    public List<PaymentHistoryViewModel> RecentPayments { get; set; } = new();
}

public class TradeWiseFeeViewModel
{
    public string TradeName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public decimal TotalFee { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public double CollectionRate => TotalFee > 0 ? (double)(CollectedAmount / TotalFee) * 100 : 0;
}