using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.ViewModels;

// Reports Index ViewModel
public class ReportsIndexViewModel
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int TotalBatches { get; set; }
    public int ActiveBatches { get; set; }
    public int TotalTrades { get; set; }
    public int ActiveTrades { get; set; }
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
}

// Student Report ViewModels
public class StudentReportViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class StudentReportListViewModel
{
    public List<StudentReportViewModel> Students { get; set; } = new();
    public int? SessionId { get; set; }
    public int? TradeId { get; set; }
    public int? BatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Session> Sessions { get; set; } = new();
    public List<Trade> Trades { get; set; } = new();
    public List<Batch> Batches { get; set; } = new();
}

// Batch Report ViewModels
public class BatchReportViewModel
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string TimingDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxStudents { get; set; }
    public int EnrolledStudents { get; set; }
    public int AvailableSlots { get; set; }
    public double FillPercentage => MaxStudents > 0 ? (double)EnrolledStudents / MaxStudents * 100 : 0;
}

public class BatchReportListViewModel
{
    public List<BatchReportViewModel> Batches { get; set; } = new();
    public int? SessionId { get; set; }
    public int? TradeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Session> Sessions { get; set; } = new();
    public List<Trade> Trades { get; set; } = new();
}

// Attendance Report ViewModels
public class AttendanceReportViewModel
{
    public DateTime Date { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public string? Remarks { get; set; }
}

public class AttendanceReportListViewModel
{
    public List<AttendanceReportViewModel> Attendances { get; set; } = new();
    public int? BatchId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Batch> Batches { get; set; } = new();
}

// Enrollment Report ViewModels
public class EnrollmentReportItem
{
    public int EnrollmentId { get; set; }
    public string RegNo { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentReportListViewModel
{
    public List<EnrollmentReportItem> Enrollments { get; set; } = new();
    public int? SessionId { get; set; }
    public int? TradeId { get; set; }
    public List<Session> Sessions { get; set; } = new();
    public List<Trade> Trades { get; set; } = new();
}

// Fee Report ViewModels
public class FeeReportViewModel
{
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string FeeStatus { get; set; } = string.Empty;
    public DateTime? LastPaymentDate { get; set; }
}

public class FeeReportListViewModel
{
    public List<FeeReportViewModel> Students { get; set; } = new();
    public int? SessionId { get; set; }
    public int? TradeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<Session> Sessions { get; set; } = new();
    public List<Trade> Trades { get; set; } = new();
    public decimal TotalFees { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}