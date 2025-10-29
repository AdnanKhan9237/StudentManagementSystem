using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using System.Text;

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
            .Where(b => !b.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(b => b.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(b => b.TradeId == tradeId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        var batchesList = await query.OrderBy(b => b.BatchCode).ToListAsync();

        // Get student counts for each batch separately
        var batches = new List<BatchReportViewModel>();
        foreach (var batch in batchesList)
        {
            var studentCount = await _context.Students
                .CountAsync(s => s.BatchId == batch.Id && !s.IsDeleted);

            batches.Add(new BatchReportViewModel
            {
                Id = batch.Id,
                BatchCode = batch.BatchCode,
                BatchName = batch.BatchName,
                SessionName = batch.Session.Name,
                TradeName = batch.Trade.NameEnglish,
                RoomNumber = batch.Room != null ? batch.Room.RoomNumber : "N/A",
                TimingDescription = batch.Timing != null ? $"{batch.Timing.Name}" : "N/A",
                Status = batch.Status,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                MaxStudents = batch.MaxStudents,
                EnrolledStudents = studentCount,
                AvailableSlots = batch.MaxStudents - studentCount
            });
        }

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
        // Use Students table since Enrollments table may be empty
        var query = _context.Students
            .Include(s => s.Session)
            .Include(s => s.Trade)
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted);

        if (sessionId.HasValue)
            query = query.Where(s => s.SessionId == sessionId);

        if (tradeId.HasValue)
            query = query.Where(s => s.TradeId == tradeId);

        var items = await query
            .OrderBy(s => s.Session.Name)
            .ThenBy(s => s.Trade.NameEnglish)
            .ThenBy(s => s.RegistrationNumber)
            .Select(s => new EnrollmentReportItem
            {
                EnrollmentId = s.Id,
                RegNo = s.RegistrationNumber,
                StudentName = s.FirstName + " " + s.LastName,
                SessionName = s.Session.Name,
                TradeName = s.Trade.NameEnglish,
                BatchCode = s.Batch != null ? s.Batch.BatchCode : "N/A",
                AdmissionDate = s.AdmissionDate,
                Status = s.Status
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

    #region Additional Reports
    // GET: Reports/FacilityReport
    public async Task<IActionResult> FacilityReport(int? sessionId, int? tradeId, string roomType = "", string building = "")
    {
        var roomsQuery = _context.Rooms.Where(r => !r.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(roomType)) roomsQuery = roomsQuery.Where(r => r.RoomType == roomType);
        if (!string.IsNullOrWhiteSpace(building)) roomsQuery = roomsQuery.Where(r => r.Building == building);

        var rooms = await roomsQuery.ToListAsync();

        // Get batches with filters
        var batchesQuery = _context.Batches.Where(b => !b.IsDeleted).AsQueryable();
        if (sessionId.HasValue) batchesQuery = batchesQuery.Where(b => b.SessionId == sessionId);
        if (tradeId.HasValue) batchesQuery = batchesQuery.Where(b => b.TradeId == tradeId);

        var batches = await batchesQuery.ToListAsync();

        // Build report items with student counts
        var items = new List<FacilityRoomReportItem>();
        foreach (var room in rooms)
        {
            var roomBatches = batches.Where(b => b.RoomId == room.Id).ToList();
            
            // Count students for all batches in this room
            var studentCount = 0;
            foreach (var batch in roomBatches)
            {
                var count = await _context.Students
                    .CountAsync(s => s.BatchId == batch.Id && !s.IsDeleted);
                studentCount += count;
            }

            items.Add(new FacilityRoomReportItem
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                RoomName = room.RoomName,
                RoomType = room.RoomType,
                Building = room.Building,
                Floor = room.Floor,
                Capacity = room.Capacity,
                ActiveBatches = roomBatches.Count,
                TotalStudents = studentCount,
                HasProjector = room.HasProjector,
                HasComputers = room.HasComputers,
                HasAirConditioning = room.HasAirConditioning
            });
        }

        items = items.OrderBy(r => r.RoomNumber).ToList();

        var model = new FacilityReportListViewModel
        {
            Rooms = items,
            SessionId = sessionId,
            TradeId = tradeId,
            RoomType = roomType,
            Building = building,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync(),
            RoomTypes = await _context.Rooms.Where(r => !r.IsDeleted && r.RoomType != null).Select(r => r.RoomType).Distinct().ToListAsync(),
            Buildings = await _context.Rooms.Where(r => !r.IsDeleted && r.Building != null).Select(r => r.Building!).Distinct().ToListAsync()
        };
        return View(model);
    }

    // GET: Reports/AcademicReport
    public async Task<IActionResult> AcademicReport(int? sessionId, int? tradeId)
    {
        var students = _context.Students
            .Include(s => s.Session)
            .Include(s => s.Trade)
            .Include(s => s.ExamResults)
            .Include(s => s.Certificates)
            .Where(s => !s.IsDeleted);
        if (sessionId.HasValue) students = students.Where(s => s.SessionId == sessionId);
        if (tradeId.HasValue) students = students.Where(s => s.TradeId == tradeId);

        var list = await students
            .Select(s => new AcademicReportItem
            {
                StudentId = s.Id,
                StudentCode = s.StudentCode,
                StudentName = s.FirstName + " " + s.LastName,
                SessionName = s.Session.Name,
                TradeName = s.Trade.NameEnglish,
                LatestPercentage = s.ExamResults
                    .OrderByDescending(er => er.EnteredDate ?? er.CreatedDate)
                    .Select(er => (decimal?)er.Percentage)
                    .FirstOrDefault(),
                LatestGrade = s.ExamResults
                    .OrderByDescending(er => er.EnteredDate ?? er.CreatedDate)
                    .Select(er => er.Grade)
                    .FirstOrDefault(),
                LatestResult = s.ExamResults
                    .OrderByDescending(er => er.EnteredDate ?? er.CreatedDate)
                    .Select(er => er.Result)
                    .FirstOrDefault(),
                LatestExamDate = s.ExamResults
                    .OrderByDescending(er => er.EnteredDate ?? er.CreatedDate)
                    .Select(er => (DateTime?)(er.EnteredDate ?? er.CreatedDate))
                    .FirstOrDefault(),
                CertificatesIssued = s.Certificates.Count(c => !c.IsDeleted),
                LastCertificateDate = s.Certificates
                    .Where(c => !c.IsDeleted)
                    .OrderByDescending(c => c.IssueDate)
                    .Select(c => (DateTime?)c.IssueDate)
                    .FirstOrDefault()
            })
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        var model = new AcademicReportListViewModel
        {
            Items = list,
            SessionId = sessionId,
            TradeId = tradeId,
            Sessions = await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            Trades = await _context.Trades.Where(t => !t.IsDeleted).ToListAsync()
        };
        return View(model);
    }
    #endregion

    #region Export Helpers
    private static string CsvEscape(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var needsQuotes = input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
        var value = input.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{value}\"" : value;
    }

    private FileContentResult CsvFile(string csv, string baseFileName)
    {
        var fileName = $"{baseFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
    #endregion

    #region Export Actions
    [HttpGet]
    public async Task<IActionResult> StudentReportCsv(int? sessionId, int? tradeId, int? batchId, string status = "")
    {
        // Build same dataset as StudentReport
        var query = _context.Students.Include(s => s.Session).Include(s => s.Trade).Include(s => s.Batch).Where(s => !s.IsDeleted);
        if (sessionId.HasValue) query = query.Where(s => s.SessionId == sessionId);
        if (tradeId.HasValue) query = query.Where(s => s.TradeId == tradeId);
        if (batchId.HasValue) query = query.Where(s => s.BatchId == batchId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

        var list = await query.OrderBy(s => s.StudentCode).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Student Code,Full Name,Email,Phone,Session,Trade,Batch,Status,Admission Date,Total Fee,Paid,Remaining");
        foreach (var s in list)
        {
            var fullName = $"{s.FirstName} {s.LastName}";
            var batch = s.Batch != null ? s.Batch.BatchCode : "N/A";
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(s.StudentCode),
                CsvEscape(fullName),
                CsvEscape(s.Email),
                CsvEscape(s.PhoneNumber),
                CsvEscape(s.Session.Name),
                CsvEscape(s.Trade.NameEnglish),
                CsvEscape(batch),
                CsvEscape(s.Status),
                CsvEscape(s.AdmissionDate.ToString("yyyy-MM-dd")),
                s.TotalFee.ToString(),
                s.PaidAmount.ToString(),
                (s.TotalFee - s.PaidAmount).ToString()
            }));
        }
        return CsvFile(sb.ToString(), "StudentReport");
    }

    [HttpGet]
    public async Task<IActionResult> BatchReportCsv(int? sessionId, int? tradeId, string status = "")
    {
        var query = _context.Batches.Include(b => b.Session).Include(b => b.Trade).Include(b => b.Room).Include(b => b.Students).Where(b => !b.IsDeleted);
        if (sessionId.HasValue) query = query.Where(b => b.SessionId == sessionId);
        if (tradeId.HasValue) query = query.Where(b => b.TradeId == tradeId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
        var list = await query.OrderBy(b => b.BatchCode).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Batch Code,Batch Name,Session,Trade,Room,Status,Start Date,End Date,Capacity,Enrolled,Available");
        foreach (var b in list)
        {
            var room = b.Room != null ? b.Room.RoomNumber : "N/A";
            var enrolled = b.Students.Count(s => !s.IsDeleted);
            var available = b.MaxStudents - enrolled;
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(b.BatchCode),
                CsvEscape(b.BatchName),
                CsvEscape(b.Session.Name),
                CsvEscape(b.Trade.NameEnglish),
                CsvEscape(room),
                CsvEscape(b.Status),
                CsvEscape(b.StartDate.ToString("yyyy-MM-dd")),
                CsvEscape(b.EndDate.ToString("yyyy-MM-dd")),
                b.MaxStudents.ToString(),
                enrolled.ToString(),
                available.ToString()
            }));
        }
        return CsvFile(sb.ToString(), "BatchReport");
    }

    [HttpGet]
    public async Task<IActionResult> AttendanceReportCsv(int? batchId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Attendances.Include(a => a.Student).Include(a => a.Batch).ThenInclude(b => b.Trade).Where(a => !a.IsDeleted);
        if (batchId.HasValue) query = query.Where(a => a.BatchId == batchId);
        if (startDate.HasValue) query = query.Where(a => a.Date >= startDate);
        if (endDate.HasValue) query = query.Where(a => a.Date <= endDate);
        var list = await query.OrderByDescending(a => a.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Student Code,Student Name,Batch,Trade,Status,Time In,Time Out,Remarks");
        foreach (var a in list)
        {
            var name = $"{a.Student.FirstName} {a.Student.LastName}";
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(a.Date.ToString("yyyy-MM-dd")),
                CsvEscape(a.Student.StudentCode),
                CsvEscape(name),
                CsvEscape(a.Batch.BatchCode),
                CsvEscape(a.Batch.Trade.NameEnglish),
                CsvEscape(a.Status),
                CsvEscape(a.TimeIn?.ToString(@"hh\:mm") ?? string.Empty),
                CsvEscape(a.TimeOut?.ToString(@"hh\:mm") ?? string.Empty),
                CsvEscape(a.Remarks ?? string.Empty)
            }));
        }
        return CsvFile(sb.ToString(), "AttendanceReport");
    }

    [HttpGet]
    public async Task<IActionResult> FeeReportCsv(int? sessionId, int? tradeId, string status = "")
    {
        var query = _context.Students.Include(s => s.Session).Include(s => s.Trade).Include(s => s.FeeTransactions).Where(s => !s.IsDeleted);
        if (sessionId.HasValue) query = query.Where(s => s.SessionId == sessionId);
        if (tradeId.HasValue) query = query.Where(s => s.TradeId == tradeId);
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Paid") query = query.Where(s => s.PaidAmount >= s.TotalFee);
            else if (status == "Partial") query = query.Where(s => s.PaidAmount > 0 && s.PaidAmount < s.TotalFee);
            else if (status == "Unpaid") query = query.Where(s => s.PaidAmount == 0);
        }
        var list = await query.OrderBy(s => s.StudentCode).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Student Code,Student Name,Session,Trade,Total Fee,Paid,Remaining,Status,Last Payment");
        foreach (var s in list)
        {
            var last = s.FeeTransactions.Where(ft => ft.TransactionType == "Payment" && !ft.IsDeleted)
                                       .OrderByDescending(ft => ft.TransactionDate)
                                       .Select(ft => (DateTime?)ft.TransactionDate)
                                       .FirstOrDefault();
            var feeStatus = s.PaidAmount >= s.TotalFee ? "Paid" : s.PaidAmount > 0 ? "Partial" : "Unpaid";
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(s.StudentCode),
                CsvEscape($"{s.FirstName} {s.LastName}"),
                CsvEscape(s.Session.Name),
                CsvEscape(s.Trade.NameEnglish),
                s.TotalFee.ToString(),
                s.PaidAmount.ToString(),
                (s.TotalFee - s.PaidAmount).ToString(),
                CsvEscape(feeStatus),
                CsvEscape(last?.ToString("yyyy-MM-dd") ?? string.Empty)
            }));
        }
        return CsvFile(sb.ToString(), "FeeReport");
    }

    [HttpGet]
    public async Task<IActionResult> EnrollmentReportCsv(int? sessionId, int? tradeId)
    {
        var query = _context.Enrollments.Include(e => e.Student).Include(e => e.Session).Include(e => e.Trade).Include(e => e.Batch).Where(e => !e.IsDeleted);
        if (sessionId.HasValue) query = query.Where(e => e.SessionId == sessionId);
        if (tradeId.HasValue) query = query.Where(e => e.TradeId == tradeId);
        var list = await query.OrderBy(e => e.Session.Name).ThenBy(e => e.Trade.NameEnglish).ThenBy(e => e.RegNo).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Reg No,Student,Session,Trade,Batch,Admission Date,Status");
        foreach (var e in list)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(e.RegNo),
                CsvEscape($"{e.Student.FirstName} {e.Student.LastName}"),
                CsvEscape(e.Session.Name),
                CsvEscape(e.Trade.NameEnglish),
                CsvEscape(e.Batch != null ? e.Batch.BatchCode : "N/A"),
                CsvEscape(e.AdmissionDate.ToString("yyyy-MM-dd")),
                CsvEscape(e.Status)
            }));
        }
        return CsvFile(sb.ToString(), "EnrollmentReport");
    }

    [HttpGet]
    public async Task<IActionResult> FacilityReportCsv(int? sessionId, int? tradeId, string roomType = "", string building = "")
    {
        var roomsQuery = _context.Rooms.Where(r => !r.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(roomType)) roomsQuery = roomsQuery.Where(r => r.RoomType == roomType);
        if (!string.IsNullOrWhiteSpace(building)) roomsQuery = roomsQuery.Where(r => r.Building == building);

        var batches = _context.Batches.Where(b => !b.IsDeleted).Include(b => b.Students).AsQueryable();
        if (sessionId.HasValue) batches = batches.Where(b => b.SessionId == sessionId);
        if (tradeId.HasValue) batches = batches.Where(b => b.TradeId == tradeId);

        var rows = await roomsQuery.Select(r => new {
            Room = r,
            BatchInfos = batches.Where(b => b.RoomId == r.Id)
        }).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Room Number,Room Name,Type,Building,Floor,Capacity,Active Batches,Total Students,Has Projector,Has Computers,AC");
        foreach (var x in rows)
        {
            var activeBatches = x.BatchInfos.Count();
            var students = x.BatchInfos.Sum(b => b.Students.Count(s => !s.IsDeleted));
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(x.Room.RoomNumber),
                CsvEscape(x.Room.RoomName ?? string.Empty),
                CsvEscape(x.Room.RoomType),
                CsvEscape(x.Room.Building ?? string.Empty),
                CsvEscape(x.Room.Floor ?? string.Empty),
                x.Room.Capacity.ToString(),
                activeBatches.ToString(),
                students.ToString(),
                CsvEscape(x.Room.HasProjector ? "Yes" : "No"),
                CsvEscape(x.Room.HasComputers ? "Yes" : "No"),
                CsvEscape(x.Room.HasAirConditioning ? "Yes" : "No")
            }));
        }
        return CsvFile(sb.ToString(), "FacilityReport");
    }

    [HttpGet]
    public async Task<IActionResult> AcademicReportCsv(int? sessionId, int? tradeId)
    {
        var students = _context.Students.Include(s => s.Session).Include(s => s.Trade).Include(s => s.ExamResults).Include(s => s.Certificates).Where(s => !s.IsDeleted);
        if (sessionId.HasValue) students = students.Where(s => s.SessionId == sessionId);
        if (tradeId.HasValue) students = students.Where(s => s.TradeId == tradeId);
        var list = await students.ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Student Code,Student,Session,Trade,Latest %,Grade,Result,Latest Exam,Certificates,Last Certificate");
        foreach (var s in list)
        {
            var latest = s.ExamResults.OrderByDescending(er => er.EnteredDate ?? er.CreatedDate).FirstOrDefault();
            var certDate = s.Certificates.Where(c => !c.IsDeleted).OrderByDescending(c => c.IssueDate).Select(c => (DateTime?)c.IssueDate).FirstOrDefault();
            sb.AppendLine(string.Join(',', new[]
            {
                CsvEscape(s.StudentCode),
                CsvEscape($"{s.FirstName} {s.LastName}"),
                CsvEscape(s.Session.Name),
                CsvEscape(s.Trade.NameEnglish),
                (latest != null ? latest.Percentage.ToString("0.##") : string.Empty),
                CsvEscape(latest?.Grade ?? string.Empty),
                CsvEscape(latest?.Result ?? string.Empty),
                CsvEscape((latest?.EnteredDate ?? latest?.CreatedDate)?.ToString("yyyy-MM-dd") ?? string.Empty),
                s.Certificates.Count(c => !c.IsDeleted).ToString(),
                CsvEscape(certDate?.ToString("yyyy-MM-dd") ?? string.Empty)
            }));
        }
        return CsvFile(sb.ToString(), "AcademicReport");
    }
    #endregion
}
