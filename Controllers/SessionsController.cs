using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class SessionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ApplicationDbContext context, ILogger<SessionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Sessions
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", bool? activeFilter = null)
    {
        const int pageSize = 10;
        
        var query = _context.Sessions
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => s.Name.Contains(searchTerm) ||
                                   s.Code.Contains(searchTerm));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(s => s.IsActive == activeFilter.Value);
        }

        var totalRecords = await query.CountAsync();

        var sessions = await query
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsCurrentSession = s.IsCurrentSession,
                IsActive = s.IsActive,
                SessionType = s.SessionType,
                DurationMonths = s.DurationMonths,
                Description = s.Description ?? string.Empty,
                CreatedDate = s.CreatedDate,
                CreatedBy = s.CreatedBy ?? string.Empty,
                StudentCount = _context.Students.Count(st => st.SessionId == s.Id && !st.IsDeleted)
            })
            .ToListAsync();

        var model = new SessionListViewModel
        {
            Sessions = sessions,
            SearchTerm = searchTerm,
            ActiveFilter = activeFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };

        return View(model);
    }

    // GET: Sessions/Details/5
    public async Task<IActionResult> Details(int id)
    {
var session = await _context.Sessions
            .Include(s => s.Batches)
                .ThenInclude(b => b.Trade)
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (session == null)
        {
            return NotFound();
        }

// Compute aggregates and batch summaries
var totalStudents = await _context.Students.CountAsync(st => st.SessionId == id && !st.IsDeleted);
var batches = await _context.Batches
    .Where(b => b.SessionId == id && !b.IsDeleted)
    .Select(b => new BatchSummaryViewModel
    {
        Id = b.Id,
        BatchCode = b.BatchCode,
        BatchName = b.BatchName,
        TradeName = b.Trade.NameEnglish,
        SessionName = session.Name,
        Status = b.Status,
        StudentCount = _context.Students.Count(s => s.BatchId == b.Id && !s.IsDeleted)
    })
    .ToListAsync();

var model = new SessionViewModel
{
    Id = session.Id,
    Name = session.Name,
    Code = session.Code,
    StartDate = session.StartDate,
    EndDate = session.EndDate,
    RegistrationStartDate = session.RegistrationStartDate,
    RegistrationEndDate = session.RegistrationEndDate,
    IsCurrentSession = session.IsCurrentSession,
    IsActive = session.IsActive,
    SessionType = session.SessionType,
    DurationMonths = session.DurationMonths,
    Description = session.Description ?? string.Empty,
    CreatedDate = session.CreatedDate,
    CreatedBy = session.CreatedBy ?? string.Empty,
    StudentCount = totalStudents,
    TotalStudents = totalStudents,
    TotalBatches = batches.Count,
    Batches = batches
};

        return View(model);
    }

    // GET: Sessions/Create
    public IActionResult Create()
    {
        var model = new SessionViewModel
        {
            StartDate = DateTime.Now.Date,
            EndDate = DateTime.Now.AddMonths(6).Date,
            IsActive = true
        };
        return View(model);
    }

    // POST: Sessions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SessionViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if code is unique
            if (await _context.Sessions.AnyAsync(s => s.Code == model.Code && !s.IsDeleted))
            {
                ModelState.AddModelError("Code", "Session code already exists.");
                return View(model);
            }

            // If setting as current session, unset others of the same type
            if (model.IsCurrentSession)
            {
                var currentSessions = await _context.Sessions
                    .Where(s => s.IsCurrentSession && s.SessionType == model.SessionType && !s.IsDeleted)
                    .ToListAsync();
                
                foreach (var session in currentSessions)
                {
                    session.IsCurrentSession = false;
                    session.ModifiedDate = DateTime.UtcNow;
                    session.ModifiedBy = User.Identity?.Name ?? "System";
                }
            }

var newSession = new Session
            {
                Name = model.Name,
                Code = model.Code,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                RegistrationStartDate = model.RegistrationStartDate,
                RegistrationEndDate = model.RegistrationEndDate,
                IsCurrentSession = model.IsCurrentSession,
                IsActive = model.IsActive,
                SessionType = model.SessionType ?? string.Empty,
                DurationMonths = model.DurationMonths,
                Description = model.Description ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Sessions.Add(newSession);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Session created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Sessions/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (session == null)
        {
            return NotFound();
        }

var model = new SessionViewModel
        {
            Id = session.Id,
            Name = session.Name,
            Code = session.Code,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            RegistrationStartDate = session.RegistrationStartDate,
            RegistrationEndDate = session.RegistrationEndDate,
            IsCurrentSession = session.IsCurrentSession,
            IsActive = session.IsActive,
            SessionType = session.SessionType,
            DurationMonths = session.DurationMonths,
            Description = session.Description
        };

        return View(model);
    }

    // POST: Sessions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SessionViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (session == null)
            {
                return NotFound();
            }

            // Check if code is unique (excluding current session)
            if (await _context.Sessions.AnyAsync(s => s.Code == model.Code && s.Id != id && !s.IsDeleted))
            {
                ModelState.AddModelError("Code", "Session code already exists.");
                return View(model);
            }

            // If setting as current session, unset others of the same type
            if (model.IsCurrentSession && !session.IsCurrentSession)
            {
                var currentSessions = await _context.Sessions
                    .Where(s => s.IsCurrentSession && s.SessionType == model.SessionType && s.Id != id && !s.IsDeleted)
                    .ToListAsync();
                
                foreach (var currentSession in currentSessions)
                {
                    currentSession.IsCurrentSession = false;
                    currentSession.ModifiedDate = DateTime.UtcNow;
                    currentSession.ModifiedBy = User.Identity?.Name ?? "System";
                }
            }

            session.Name = model.Name;
            session.Code = model.Code;
session.StartDate = model.StartDate;
            session.EndDate = model.EndDate;
            session.RegistrationStartDate = model.RegistrationStartDate;
            session.RegistrationEndDate = model.RegistrationEndDate;
            session.IsCurrentSession = model.IsCurrentSession;
            session.IsActive = model.IsActive;
            session.SessionType = model.SessionType;
            session.DurationMonths = model.DurationMonths;
            session.Description = model.Description;
            session.ModifiedDate = DateTime.UtcNow;
            session.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Session updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Sessions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (session == null)
        {
            return NotFound();
        }

        var model = new SessionViewModel
        {
            Id = session.Id,
            Name = session.Name,
            Code = session.Code,
            StartDate = session.StartDate,
            EndDate = session.EndDate,
            IsCurrentSession = session.IsCurrentSession,
            IsActive = session.IsActive,
            SessionType = session.SessionType,
            DurationMonths = session.DurationMonths,
            Description = session.Description,
            StudentCount = await _context.Students.CountAsync(s => s.SessionId == id && !s.IsDeleted)
        };

        return View(model);
    }

    // POST: Sessions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (session == null)
        {
            return NotFound();
        }

        // Check if session has students
        var hasStudents = await _context.Students.AnyAsync(s => s.SessionId == id && !s.IsDeleted);
        if (hasStudents)
        {
            TempData["ErrorMessage"] = "Cannot delete session that has enrolled students.";
            return RedirectToAction(nameof(Index));
        }

        session.IsDeleted = true;
        session.ModifiedDate = DateTime.UtcNow;
        session.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Session deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // AJAX: Set Current Session
    [HttpPost]
    public async Task<IActionResult> SetCurrentSession(int id)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (session == null)
        {
            return Json(new { success = false, message = "Session not found." });
        }

        // Unset all current sessions
        var currentSessions = await _context.Sessions
            .Where(s => s.IsCurrentSession && !s.IsDeleted)
            .ToListAsync();
        
        foreach (var currentSession in currentSessions)
        {
            currentSession.IsCurrentSession = false;
            currentSession.ModifiedDate = DateTime.UtcNow;
            currentSession.ModifiedBy = User.Identity?.Name ?? "System";
        }

        // Set new current session
        session.IsCurrentSession = true;
        session.ModifiedDate = DateTime.UtcNow;
        session.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Current session updated successfully!" });
    }
}