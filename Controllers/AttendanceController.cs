using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

// GET: Attendance
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var batches = await GetUserBatchesAsync(currentUserId);

        var model = new AttendanceIndexViewModel
        {
            Batches = batches,
            TodayDate = DateTime.Today
        };

        return View(model);
    }

// GET: Attendance/TakeAttendance/5?date=2024-01-01
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> TakeAttendance(int batchId, DateTime? date = null)
    {
        date ??= DateTime.Today;

        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Timing)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            return NotFound();
        }

        // Check if user has permission to mark attendance for this batch
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(UserRoles.SuperAdmin) && !await HasBatchAccessAsync(currentUserId, batchId))
        {
            return Forbid("You can only mark attendance for your assigned batches.");
        }

        // Get students enrolled in this batch
        var students = await _context.Students
            .Where(s => s.BatchId == batchId && !s.IsDeleted)
            .OrderBy(s => s.RegistrationNumber)
            .ToListAsync();

        _logger.LogInformation("Batch {BatchId}: Found {Count} students", batchId, students.Count);

        // Prefetch enrollments for this batch's session/trade
        var enrollmentsMap = await _context.Enrollments
            .AsNoTracking()
            .Where(e => students.Select(s => s.Id).Contains(e.StudentId)
                         && e.SessionId == batch.SessionId
                         && e.TradeId == batch.TradeId)
            .ToDictionaryAsync(e => e.StudentId, e => e);

        // Get existing attendance for this date
        var existingAttendance = await _context.Attendances
            .Where(a => a.BatchId == batchId && a.Date.Date == date.Value.Date)
            .ToListAsync();

        var model = new TakeAttendanceViewModel
        {
            BatchId = batchId,
            BatchName = batch.BatchName,
            BatchCode = batch.BatchCode,
            TradeName = batch.Trade?.NameEnglish ?? "N/A",
            SessionName = batch.Session?.Name ?? "N/A",
            TimingName = batch.Timing != null ? $"{batch.Timing.StartTime:HH:mm} - {batch.Timing.EndTime:HH:mm}" : "N/A",
            RoomName = batch.Room != null ? (batch.Room.RoomName ?? batch.Room.RoomNumber) : "N/A",
            AttendanceDate = date.Value,
            Students = students.Select(s => {
                enrollmentsMap.TryGetValue(s.Id, out var enr);
                var existing = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id);
                return new StudentAttendanceViewModel
                {
                    StudentId = s.Id,
                    EnrollmentId = enr?.Id,
                    RegistrationNumber = enr?.RegNo ?? s.RegistrationNumber,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    Status = existing?.Status ?? "Present",
                    TimeIn = existing?.TimeIn,
                    TimeOut = existing?.TimeOut,
                    Remarks = existing?.Remarks ?? ""
                };
            }).ToList()
        };

        return View(model);
    }

// POST: Attendance/TakeAttendance
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> TakeAttendance(TakeAttendanceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var batch = await _context.Batches.FirstOrDefaultAsync(b => b.Id == model.BatchId);
        if (batch == null)
        {
            return NotFound();
        }

        // Check permissions
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(UserRoles.SuperAdmin) && !await HasBatchAccessAsync(currentUserId, model.BatchId))
        {
            return Forbid();
        }

        try
        {
            // Remove existing attendance for this date
            var existingAttendance = await _context.Attendances
                .Where(a => a.BatchId == model.BatchId && a.Date.Date == model.AttendanceDate.Date)
                .ToListAsync();

            _context.Attendances.RemoveRange(existingAttendance);

            // Resolve enrollments for this batch
            var studentIds = model.Students.Select(s => s.StudentId).ToList();
            var enrollmentsMap = await _context.Enrollments
                .AsNoTracking()
                .Where(e => studentIds.Contains(e.StudentId) && e.SessionId == batch.SessionId && e.TradeId == batch.TradeId)
                .ToDictionaryAsync(e => e.StudentId, e => e.Id);

            // Add new attendance records
            foreach (var student in model.Students)
            {
                enrollmentsMap.TryGetValue(student.StudentId, out var enrollmentId);

                var attendance = new Attendance
                {
                    StudentId = student.StudentId,
                    BatchId = model.BatchId,
                    EnrollmentId = student.EnrollmentId ?? enrollmentId,
                    Date = model.AttendanceDate,
                    Status = student.Status,
                    TimeIn = student.TimeIn,
                    TimeOut = student.TimeOut,
                    Remarks = student.Remarks,
                    MarkedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserId ?? "System"
                };

                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Attendance for {model.AttendanceDate:dd/MM/yyyy} has been successfully recorded for {model.Students.Count} students.";
            return RedirectToAction(nameof(ViewAttendance), new { batchId = model.BatchId, date = model.AttendanceDate });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving attendance for batch {BatchId} on {Date}", model.BatchId, model.AttendanceDate);
            ModelState.AddModelError("", "An error occurred while saving attendance. Please try again.");
            return View(model);
        }
    }

// GET: Attendance/ViewAttendance/5?date=2024-01-01
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> ViewAttendance(int batchId, DateTime? date = null)
    {
        date ??= DateTime.Today;

        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Timing)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            return NotFound();
        }

        // Check permissions
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(UserRoles.SuperAdmin) && !await HasBatchAccessAsync(currentUserId, batchId))
        {
            return Forbid();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Enrollment)
            .Include(a => a.MarkedByUser)
            .Where(a => a.BatchId == batchId && a.Date.Date == date.Value.Date)
            .OrderBy(a => a.Enrollment != null ? a.Enrollment.RegNo : a.Student.RegistrationNumber)
            .ToListAsync();

        var model = new ViewAttendanceViewModel
        {
            BatchId = batchId,
            BatchName = batch.BatchName,
            BatchCode = batch.BatchCode,
            TradeName = batch.Trade?.NameEnglish ?? "N/A",
            SessionName = batch.Session?.Name ?? "N/A",
            AttendanceDate = date.Value,
            AttendanceRecords = attendance.Select(a => new AttendanceRecordViewModel
            {
                StudentId = a.StudentId,
                EnrollmentId = a.EnrollmentId,
                RegistrationNumber = a.Enrollment != null ? a.Enrollment.RegNo : a.Student.RegistrationNumber,
                StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                Status = a.Status,
                TimeIn = a.TimeIn,
                TimeOut = a.TimeOut,
                Remarks = a.Remarks ?? string.Empty,
                MarkedBy = a.MarkedByUser?.UserName ?? "System",
                MarkedOn = a.CreatedDate
            }).ToList(),
            TotalStudents = attendance.Count,
            PresentCount = attendance.Count(a => a.Status == "Present"),
            AbsentCount = attendance.Count(a => a.Status == "Absent"),
            LateCount = attendance.Count(a => a.Status == "Late"),
            ExcusedCount = attendance.Count(a => a.Status == "Excused")
        };

        return View(model);
    }

// GET: Attendance/BatchReport/5
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> BatchReport(int batchId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        fromDate ??= DateTime.Today.AddDays(-30); // Default to last 30 days
        toDate ??= DateTime.Today;

        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Students.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            return NotFound();
        }

        // Check permissions
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(UserRoles.SuperAdmin) && !await HasBatchAccessAsync(currentUserId, batchId))
        {
            return Forbid();
        }

        // Get attendance data for the date range
        var attendanceData = await _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Enrollment)
            .Where(a => a.BatchId == batchId && a.Date >= fromDate && a.Date <= toDate)
            .GroupBy(a => a.EnrollmentId ?? 0)
            .Select(g => new StudentAttendanceReportViewModel
            {
                StudentId = g.First().StudentId,
                RegistrationNumber = g.First().Enrollment != null ? g.First().Enrollment.RegNo : g.First().Student.RegistrationNumber,
                StudentName = $"{g.First().Student.FirstName} {g.First().Student.LastName}",
                TotalDays = g.Count(),
                PresentDays = g.Count(a => a.Status == "Present"),
                AbsentDays = g.Count(a => a.Status == "Absent"),
                LateDays = g.Count(a => a.Status == "Late"),
                ExcusedDays = g.Count(a => a.Status == "Excused")
            })
            .ToListAsync();

        // Calculate attendance percentage for each student
        foreach (var student in attendanceData)
        {
            student.AttendancePercentage = student.TotalDays > 0 
                ? (double)(student.PresentDays + student.LateDays) / student.TotalDays * 100 
                : 0;
        }

        // Get daily attendance summary
        var dailySummary = await _context.Attendances
            .Where(a => a.BatchId == batchId && a.Date >= fromDate && a.Date <= toDate)
            .GroupBy(a => a.Date.Date)
            .Select(g => new DailyAttendanceSummaryViewModel
            {
                Date = g.Key,
                TotalStudents = g.Count(),
                PresentCount = g.Count(a => a.Status == "Present"),
                AbsentCount = g.Count(a => a.Status == "Absent"),
                LateCount = g.Count(a => a.Status == "Late"),
                ExcusedCount = g.Count(a => a.Status == "Excused")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var model = new BatchAttendanceReportViewModel
        {
            BatchId = batchId,
            BatchName = batch.BatchName,
            BatchCode = batch.BatchCode,
            TradeName = batch.Trade?.NameEnglish ?? "N/A",
            SessionName = batch.Session?.Name ?? "N/A",
            FromDate = fromDate.Value,
            ToDate = toDate.Value,
            TotalStudentsInBatch = batch.Students.Count,
            StudentAttendance = attendanceData.OrderBy(s => s.RegistrationNumber).ToList(),
            DailySummary = dailySummary,
            OverallAttendancePercentage = attendanceData.Any() ? attendanceData.Average(s => s.AttendancePercentage) : 0
        };

        return View(model);
    }

// GET: Attendance/StudentReport/5
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher},{UserRoles.Student}")]
    public async Task<IActionResult> StudentReport(int studentId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        fromDate ??= DateTime.Today.AddDays(-30);
        toDate ??= DateTime.Today;

        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            return NotFound();
        }

        // Check permissions
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole(UserRoles.Student))
        {
            // Students can only view their own attendance
            if (student.UserId != currentUserId)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole(UserRoles.SuperAdmin) && student.BatchId.HasValue && !await HasBatchAccessAsync(currentUserId, student.BatchId.Value))
        {
            return Forbid();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Batch)
            .Include(a => a.Enrollment)
            .Include(a => a.MarkedByUser)
            .Where(a => a.StudentId == studentId && a.Date >= fromDate && a.Date <= toDate)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        var model = new StudentAttendanceReportViewModel
        {
            StudentId = studentId,
            RegistrationNumber = attendance.FirstOrDefault()?.Enrollment?.RegNo ?? student.RegistrationNumber,
            StudentName = $"{student.FirstName} {student.LastName}",
            TradeName = student.Trade.NameEnglish,
            SessionName = student.Session.Name,
            BatchName = student.Batch?.BatchName ?? "Not Assigned",
            FromDate = fromDate.Value,
            ToDate = toDate.Value,
            TotalDays = attendance.Count,
            PresentDays = attendance.Count(a => a.Status == "Present"),
            AbsentDays = attendance.Count(a => a.Status == "Absent"),
            LateDays = attendance.Count(a => a.Status == "Late"),
            ExcusedDays = attendance.Count(a => a.Status == "Excused"),
            AttendanceRecords = attendance.Select(a => new AttendanceRecordViewModel
            {
                Date = a.Date,
                Status = a.Status,
                TimeIn = a.TimeIn,
                TimeOut = a.TimeOut,
                Remarks = a.Remarks ?? string.Empty,
                MarkedBy = a.MarkedByUser?.UserName ?? "System",
                MarkedOn = a.CreatedDate,
                BatchName = a.Batch.BatchName
            }).ToList()
        };

        model.AttendancePercentage = model.TotalDays > 0 
            ? (double)(model.PresentDays + model.LateDays) / model.TotalDays * 100 
            : 0;

        return View(model);
    }

    // Helper method to get batches for current user
    private async Task<List<AttendanceBatchListViewModel>> GetUserBatchesAsync(string? userId)
    {
        IQueryable<Batch> query = _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Students)
            .Where(b => !b.IsDeleted);

        // If not SuperAdmin, filter by assigned teacher batches
        if (!User.IsInRole(UserRoles.SuperAdmin))
        {
            var teacherBatches = await GetTeacherBatchIdsAsync(userId);
            query = query.Where(b => teacherBatches.Contains(b.Id));
        }

        return await query
            .Select(b => new AttendanceBatchListViewModel
            {
                BatchId = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                TradeName = b.Trade.NameEnglish,
                SessionName = b.Session.Name,
                StudentCount = b.Students.Count(s => !s.IsDeleted),
                Status = b.Status
            })
            .OrderBy(b => b.BatchCode)
            .ToListAsync();
    }

    // API endpoint for quick attendance status
    [HttpGet]
    public async Task<IActionResult> GetAttendanceStatus(int batchId, DateTime date)
    {
        var attendanceCount = await _context.Attendances
            .Where(a => a.BatchId == batchId && a.Date.Date == date.Date)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Json(attendanceCount);
    }

    // Helper method to check if a teacher has access to a batch
    private async Task<bool> HasBatchAccessAsync(string? userId, int batchId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);

        if (teacher == null)
            return false;

        return await _context.Batches
            .AnyAsync(b => b.Id == batchId && 
                          (b.PrimaryInstructorId == teacher.Id || b.SecondaryInstructorId == teacher.Id) &&
                          !b.IsDeleted);
    }

    // Helper method to get batch IDs for a teacher
    private async Task<List<int>> GetTeacherBatchIdsAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new List<int>();

        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);

        if (teacher == null)
            return new List<int>();

        return await _context.Batches
            .Where(b => (b.PrimaryInstructorId == teacher.Id || b.SecondaryInstructorId == teacher.Id) &&
                       !b.IsDeleted)
            .Select(b => b.Id)
            .ToListAsync();
    }
}
