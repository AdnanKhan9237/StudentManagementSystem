using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin},{UserRoles.Teacher}")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Reports
    public async Task<IActionResult> Index()
    {
        var model = new ReportsIndexViewModel
        {
            TotalStudents = await _context.Students.CountAsync(s => !s.IsDeleted),
            ActiveStudents = await _context.Students.CountAsync(s => s.Status == "Active" && !s.IsDeleted),
            TotalBatches = await _context.Batches.CountAsync(b => !b.IsDeleted),
            ActiveBatches = await _context.Batches.CountAsync(b => b.Status == "Active" && !b.IsDeleted),
            TotalTrades = await _context.Trades.CountAsync(t => !t.IsDeleted),
            ActiveTrades = await _context.Trades.CountAsync(t => t.IsActive && !t.IsDeleted),
            TotalSessions = await _context.Sessions.CountAsync(s => !s.IsDeleted),
            ActiveSessions = await _context.Sessions.CountAsync(s => s.IsActive && !s.IsDeleted),
            TotalRooms = await _context.Rooms.CountAsync(r => !r.IsDeleted),
            ActiveRooms = await _context.Rooms.CountAsync(r => r.IsActive && !r.IsDeleted)
        };

        return View(model);
    }

    // GET: Reports/StudentReport
    public async Task<IActionResult> StudentReport(int? sessionId, int? tradeId, int? batchId, string status = "")
    {
        var query = _context.Students
            .Include(s => s.Session)
            .Include(s => s.Trade)
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(s => s.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(s => s.TradeId == tradeId);

        if (batchId.HasValue)
            query = query.Where(s => s.BatchId == batchId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var students = await query
            .OrderBy(s => s.StudentCode)
            .Select(s => new StudentReportViewModel
            {
                Id = s.Id,
                StudentCode = s.StudentCode,
                FullName = $"{s.FirstName} {s.LastName}",
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                SessionName = s.Session.Name,
                TradeName = s.Trade.NameEnglish,
                BatchCode = s.Batch != null ? s.Batch.BatchCode : "N/A",
                Status = s.Status,
                AdmissionDate = s.AdmissionDate,
                TotalFee = s.TotalFee,
                PaidAmount = s.PaidAmount,
                RemainingAmount = s.TotalFee - s.PaidAmount
            })
            .ToListAsync();

        var model = new StudentReportListViewModel
        {
            Students = students,
            SessionId = sessionId,
            TradeId = tradeId,
            BatchId = batchId,
            Status = status,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync(),
            Batches = await _context.Batches.Where(b => !b.IsDeleted).ToListAsync()
        };

        return View(model);
    }

    // GET: Reports/BatchReport
    public async Task<IActionResult> BatchReport(int? sessionId, int? tradeId, string status = "")
    {
        var query = _context.Batches
            .Include(b => b.Session)
            .Include(b => b.Trade)
            .Include(b => b.Room)
            .Include(b => b.Timing)
            .Include(b => b.Students)
            .Where(b => !b.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(b => b.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(b => b.TradeId == tradeId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        var batches = await query
            .OrderBy(b => b.BatchCode)
            .Select(b => new BatchReportViewModel
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                SessionName = b.Session.Name,
                TradeName = b.Trade.NameEnglish,
                RoomNumber = b.Room != null ? b.Room.RoomNumber : "N/A",
                TimingDescription = b.Timing != null ? $"{b.Timing.Name}" : "N/A",
                Status = b.Status,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                MaxStudents = b.MaxStudents,
                EnrolledStudents = b.Students.Count(s => !s.IsDeleted),
                AvailableSlots = b.MaxStudents - b.Students.Count(s => !s.IsDeleted)
            })
            .ToListAsync();

        var model = new BatchReportListViewModel
        {
            Batches = batches,
            SessionId = sessionId,
            TradeId = tradeId,
            Status = status,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync()
        };

        return View(model);
    }

    // GET: Reports/EnrollmentReport
    public async Task<IActionResult> EnrollmentReport(int? sessionId, int? tradeId)
    {
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Include(e => e.Trade)
            .Include(e => e.Batch)
            .Where(e => !e.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(e => e.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(e => e.TradeId == tradeId);

        var items = await query
            .OrderBy(e => e.Session.Name)
            .ThenBy(e => e.Trade.NameEnglish)
            .ThenBy(e => e.RegNo)
            .Select(e => new EnrollmentReportItem
            {
                EnrollmentId = e.Id,
                RegNo = e.RegNo,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                SessionName = e.Session.Name,
                TradeName = e.Trade.NameEnglish,
                BatchCode = e.Batch != null ? e.Batch.BatchCode : "N/A",
                AdmissionDate = e.AdmissionDate,
                Status = e.Status
            })
            .ToListAsync();

        var model = new EnrollmentReportListViewModel
        {
            Enrollments = items,
            SessionId = sessionId,
            TradeId = tradeId,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync()
        };

        return View(model);
    }

    // GET: Reports/AttendanceReport
    public async Task<IActionResult> AttendanceReport(int? batchId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Batch)
            .ThenInclude(b => b.Trade)
            .Where(a => !a.IsDeleted);

        if (batchId.HasValue)
            query = query.Where(a => a.BatchId == batchId);

        if (startDate.HasValue)
            query = query.Where(a => a.Date >= startDate);

        if (endDate.HasValue)
            query = query.Where(a => a.Date <= endDate);

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .Select(a => new AttendanceReportViewModel
            {
                Date = a.Date,
                StudentCode = a.Student.StudentCode,
                StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                BatchCode = a.Batch.BatchCode,
                TradeName = a.Batch.Trade.NameEnglish,
                Status = a.Status,
                TimeIn = a.TimeIn,
                TimeOut = a.TimeOut,
                Remarks = a.Remarks
            })
            .ToListAsync();

        var model = new AttendanceReportListViewModel
        {
            Attendances = attendances,
            BatchId = batchId,
            StartDate = startDate,
            EndDate = endDate,
            Batches = await _context.Batches.Where(b => !b.IsDeleted).ToListAsync()
        };

        return View(model);
    }

    // GET: Reports/FeeReport
    public async Task<IActionResult> FeeReport(int? sessionId, int? tradeId, string status = "")
    {
        var query = _context.Students
            .Include(s => s.Session)
            .Include(s => s.Trade)
            .Include(s => s.FeeTransactions)
            .Where(s => !s.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(s => s.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(s => s.TradeId == tradeId);

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Paid")
                query = query.Where(s => s.PaidAmount >= s.TotalFee);
            else if (status == "Partial")
                query = query.Where(s => s.PaidAmount > 0 && s.PaidAmount < s.TotalFee);
            else if (status == "Unpaid")
                query = query.Where(s => s.PaidAmount == 0);
        }

        var students = await query
            .OrderBy(s => s.StudentCode)
            .Select(s => new FeeReportViewModel
            {
                StudentCode = s.StudentCode,
                StudentName = $"{s.FirstName} {s.LastName}",
                SessionName = s.Session.Name,
                TradeName = s.Trade.NameEnglish,
                TotalFee = s.TotalFee,
                PaidAmount = s.PaidAmount,
                RemainingAmount = s.TotalFee - s.PaidAmount,
                FeeStatus = s.PaidAmount >= s.TotalFee ? "Paid" : 
                           s.PaidAmount > 0 ? "Partial" : "Unpaid",
                LastPaymentDate = s.FeeTransactions
                    .Where(ft => ft.TransactionType == "Payment" && !ft.IsDeleted)
                    .OrderByDescending(ft => ft.TransactionDate)
                    .Select(ft => ft.TransactionDate)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var model = new FeeReportListViewModel
        {
            Students = students,
            SessionId = sessionId,
            TradeId = tradeId,
            Status = status,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync(),
            TotalFees = students.Sum(s => s.TotalFee),
            TotalPaid = students.Sum(s => s.PaidAmount),
            TotalRemaining = students.Sum(s => s.RemainingAmount)
        };

        return View(model);
    }
}