using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class AttendanceIndexViewModel
{
    public List<AttendanceBatchListViewModel> Batches { get; set; } = new();
    public DateTime TodayDate { get; set; }
}

public class AttendanceBatchListViewModel
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TakeAttendanceViewModel
{
    public int BatchId { get; set; }
    public int? TimingId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string TimingName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Attendance date is required")]
    public DateTime AttendanceDate { get; set; }

    public List<StudentAttendanceViewModel> Students { get; set; } = new();
}

public class StudentAttendanceViewModel
{
    public int StudentId { get; set; }
    public int? EnrollmentId { get; set; } // New: selected enrollment for this batch
    public string RegistrationNumber { get; set; } = string.Empty; // Enrollment RegNo
    public string StudentName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Attendance status is required")]
    public string Status { get; set; } = "Present"; // Present, Absent, Late, Excused
    
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    
    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
    public string Remarks { get; set; } = string.Empty;
}

public class ViewAttendanceViewModel
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    
    public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
    
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    
    public double AttendancePercentage => TotalStudents > 0 ? (double)(PresentCount + LateCount) / TotalStudents * 100 : 0;
}

public class AttendanceRecordViewModel
{
    public int StudentId { get; set; }
    public int? EnrollmentId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty; // Enrollment RegNo
    public string StudentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? TimeIn { get; set; }
    public TimeSpan? TimeOut { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string MarkedBy { get; set; } = string.Empty;
    public DateTime MarkedOn { get; set; }
    public string BatchName { get; set; } = string.Empty;
}

public class BatchAttendanceReportViewModel
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalStudentsInBatch { get; set; }
    public double OverallAttendancePercentage { get; set; }
    
    public List<StudentAttendanceReportViewModel> StudentAttendance { get; set; } = new();
    public List<DailyAttendanceSummaryViewModel> DailySummary { get; set; } = new();
}

public class StudentAttendanceReportViewModel
{
    public int StudentId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int ExcusedDays { get; set; }
    public double AttendancePercentage { get; set; }
    
    public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
}

public class DailyAttendanceSummaryViewModel
{
    public DateTime Date { get; set; }
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    
    public double AttendancePercentage => TotalStudents > 0 ? (double)(PresentCount + LateCount) / TotalStudents * 100 : 0;
}