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

    // GET: Attendance/SelectTiming/5?date=2024-01-01
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> SelectTiming(int batchId, DateTime? date = null)
    {
        date ??= DateTime.Today;

        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            return NotFound();
        }

        // Get all timings for this batch (students assigned to different timings)
        var timingsWithStudents = await _context.Students
            .Where(s => s.BatchId == batchId && s.TimingId.HasValue && !s.IsDeleted)
            .GroupBy(s => s.TimingId)
            .Select(g => new
            {
                TimingId = g.Key!.Value,
                StudentCount = g.Count()
            })
            .ToListAsync();

        var timingIds = timingsWithStudents.Select(t => t.TimingId).ToList();
        var timings = await _context.Timings
            .Where(t => timingIds.Contains(t.Id))
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        var model = new
        {
            BatchId = batchId,
            BatchName = batch.BatchName,
            BatchCode = batch.BatchCode,
            TradeName = batch.Trade?.NameEnglish,
            SessionName = batch.Session?.Name,
            Date = date.Value,
            Timings = timings.Select(t => new
            {
                TimingId = t.Id,
                TimingName = $"{t.Name} ({t.StartTime:hh\\:mm} - {t.EndTime:hh\\:mm})",
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                StudentCount = timingsWithStudents.FirstOrDefault(ts => ts.TimingId == t.Id)?.StudentCount ?? 0
            }).ToList()
        };

        return View(model);
    }

// GET: Attendance/TakeAttendance/5?timingId=1&date=2024-01-01
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Teacher}")]
    public async Task<IActionResult> TakeAttendance(int batchId, int? timingId = null, DateTime? date = null)
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

        // Get students enrolled in this batch and timing
        var studentsQuery = _context.Students
            .Where(s => s.BatchId == batchId && !s.IsDeleted);
        
        // Filter by timing if specified
        if (timingId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.TimingId == timingId.Value);
        }
        
        var students = await studentsQuery
            .OrderBy(s => s.RegistrationNumber)
            .ToListAsync();

        _logger.LogInformation("Batch {BatchId}, Timing {TimingId}: Found {Count} students", batchId, timingId, students.Count);

        // Get timing information if timingId is provided
        Timing? timing = null;
        if (timingId.HasValue)
        {
            timing = await _context.Timings.FindAsync(timingId.Value);
        }
        else
        {
            timing = batch.Timing;
        }

        // Get existing attendance for this date and timing
        var existingAttendanceQuery = _context.Attendances
            .Where(a => a.BatchId == batchId && a.Date.Date == date.Value.Date);
        
        if (timingId.HasValue)
        {
            existingAttendanceQuery = existingAttendanceQuery.Where(a => a.TimingId == timingId.Value);
        }
        
        var existingAttendance = await existingAttendanceQuery.ToListAsync();

        var model = new TakeAttendanceViewModel
        {
            BatchId = batchId,
            TimingId = timingId,
            BatchName = batch.BatchName,
            BatchCode = batch.BatchCode,
            TradeName = batch.Trade?.NameEnglish ?? "N/A",
            SessionName = batch.Session?.Name ?? "N/A",
            TimingName = timing != null ? $"{timing.StartTime:hh\\:mm} - {timing.EndTime:hh\\:mm}" : "N/A",
            RoomName = batch.Room != null ? (batch.Room.RoomName ?? batch.Room.RoomNumber) : "N/A",
            AttendanceDate = date.Value,
            Students = students.Select(s => {
                var existing = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id);
                return new StudentAttendanceViewModel
                {
                    StudentId = s.Id,
                    EnrollmentId = null,
                    RegistrationNumber = s.RegistrationNumber,
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
            // Remove existing attendance for this date and timing
            var existingAttendanceQuery = _context.Attendances
                .Where(a => a.BatchId == model.BatchId && a.Date.Date == model.AttendanceDate.Date);
            
            if (model.TimingId.HasValue)
            {
                existingAttendanceQuery = existingAttendanceQuery.Where(a => a.TimingId == model.TimingId.Value);
            }
            
            var existingAttendance = await existingAttendanceQuery.ToListAsync();
            _context.Attendances.RemoveRange(existingAttendance);

            // Add new attendance records
            foreach (var student in model.Students)
            {
                var attendance = new Attendance
                {
                    StudentId = student.StudentId,
                    BatchId = model.BatchId,
                    TimingId = model.TimingId,
                    EnrollmentId = null, // Enrollments table not in use
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
.OrderBy(a => a.Enrollment != null ? a.Enrollment.RegNo : (a.Student != null ? a.Student.RegistrationNumber : string.Empty))
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
RegistrationNumber = a.Enrollment != null ? a.Enrollment.RegNo : (a.Student != null ? a.Student.RegistrationNumber : string.Empty),
StudentName = $"{(a.Student != null ? a.Student.FirstName : string.Empty)} {(a.Student != null ? a.Student.LastName : string.Empty)}",
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
            .GroupBy(a => a.StudentId)
            .Select(g => new StudentAttendanceReportViewModel
            {
                StudentId = g.Key,
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
BatchName = a.Batch?.BatchName ?? "N/A"
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
            .Where(b => !b.IsDeleted);

        // If not SuperAdmin, filter by assigned teacher batches
        if (!User.IsInRole(UserRoles.SuperAdmin))
        {
            var teacherBatches = await GetTeacherBatchIdsAsync(userId);
            query = query.Where(b => teacherBatches.Contains(b.Id));
        }

        var batches = await query.ToListAsync();
        
        var result = new List<AttendanceBatchListViewModel>();
        foreach (var batch in batches)
        {
            var studentCount = await _context.Students
                .CountAsync(s => s.BatchId == batch.Id && !s.IsDeleted);
            
            result.Add(new AttendanceBatchListViewModel
            {
                BatchId = batch.Id,
                BatchCode = batch.BatchCode,
                BatchName = batch.BatchName,
                TradeName = batch.Trade.NameEnglish,
                SessionName = batch.Session.Name,
                StudentCount = studentCount,
                Status = batch.Status
            });
        }
        
        return result.OrderBy(b => b.BatchCode).ToList();
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
