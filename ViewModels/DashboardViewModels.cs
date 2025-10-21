namespace StudentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public string DashboardType { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalTrades { get; set; }
        public int TotalBatches { get; set; }
        public int TotalTeachers { get; set; }
        public int ActiveTeachers { get; set; }
        public int TotalUsers { get; set; }
        public int StudentsPendingFees { get; set; }
        
        public decimal TotalFeeAmount { get; set; }
        public decimal CollectedFeeAmount { get; set; }
        public decimal PendingFeeAmount { get; set; }
        public double CollectionRate => TotalFeeAmount > 0 ? (double)CollectedFeeAmount / (double)TotalFeeAmount * 100 : 0;
        
        public List<RecentStudentDto> RecentStudents { get; set; } = new();
        public List<StatusCountDto> StudentsByStatus { get; set; } = new();
        public List<TradeCountDto> StudentsByTrade { get; set; } = new();
        public List<MonthlyAdmissionDto> MonthlyAdmissions { get; set; } = new();
        public List<TradeCapacityDto> LowCapacityTrades { get; set; } = new();
        public StudentProfileDto? CurrentStudent { get; set; }
    }

    public class StudentProfileDto
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? PhotoPath { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalFee { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime AdmissionDate { get; set; }
    }

    public class RecentStudentDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public string? PhotoPath { get; set; }
    }

    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TradeCountDto
    {
        public string TradeName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyAdmissionDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class TradeCapacityDto
    {
        public int Id { get; set; }
        public string TradeName { get; set; } = string.Empty;
        public int MaxStudents { get; set; }
        public int CurrentStudents { get; set; }
        public double FillPercentage { get; set; }
        public int AvailableSlots => MaxStudents - CurrentStudents;
    }
}