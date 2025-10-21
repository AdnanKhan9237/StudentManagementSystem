using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class BatchController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BatchController> _logger;

    public BatchController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BatchController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: Batch
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "")
    {
        const int pageSize = 15;
        
        var query = _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.PrimaryInstructor)
            .Include(b => b.SecondaryInstructor)
            .Include(b => b.Students)
            .Where(b => !b.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => b.BatchName.Contains(searchTerm) ||
                                   b.BatchCode.Contains(searchTerm));
        }

        if (tradeFilter.HasValue)
        {
            query = query.Where(b => b.TradeId == tradeFilter.Value);
        }

        if (sessionFilter.HasValue)
        {
            query = query.Where(b => b.SessionId == sessionFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(b => b.Status == statusFilter);
        }

        var totalRecords = await query.CountAsync();

        var batches = await query
            .OrderBy(b => b.BatchCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BatchViewModel
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                TradeId = b.TradeId,
                TradeName = b.Trade.NameEnglish,
                SessionId = b.SessionId,
                SessionName = b.Session.Name,
                PrimaryInstructorId = b.PrimaryInstructorId,
                PrimaryInstructorName = b.PrimaryInstructor != null ? $"{b.PrimaryInstructor.FirstName} {b.PrimaryInstructor.LastName}" : "Not Assigned",
                SecondaryInstructorId = b.SecondaryInstructorId,
                SecondaryInstructorName = b.SecondaryInstructor != null ? $"{b.SecondaryInstructor.FirstName} {b.SecondaryInstructor.LastName}" : "Not Assigned",
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                MaxStudents = b.MaxStudents,
                CurrentEnrollment = b.Students.Count(s => !s.IsDeleted),
                Status = b.Status,
                CreatedDate = b.CreatedDate,
                CreatedBy = b.CreatedBy
            })
            .ToListAsync();

        // Get filter options
        var trades = await _context.Trades
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish })
            .ToListAsync();

        var sessions = await _context.Sessions
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        var model = new BatchListViewModel
        {
            Batches = batches,
            SearchTerm = searchTerm,
            TradeFilter = tradeFilter,
            SessionFilter = sessionFilter,
            StatusFilter = statusFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            Trades = trades.Cast<dynamic>().ToList(),
            Sessions = sessions.Cast<dynamic>().ToList(),
            StatusOptions = new SelectList(new[]
            {
                new { Value = "", Text = "All Status" },
                new { Value = "Active", Text = "Active" },
                new { Value = "Completed", Text = "Completed" },
                new { Value = "Cancelled", Text = "Cancelled" }
            }, "Value", "Text")
        };

        return View(model);
    }

    // GET: Batch/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Timing)
            .Include(b => b.Room)
            .Include(b => b.PrimaryInstructor)
            .Include(b => b.SecondaryInstructor)
            .Include(b => b.Students.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (batch == null)
        {
            return NotFound();
        }

        var model = new BatchViewModel
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            TradeId = batch.TradeId,
            TradeName = batch.Trade.NameEnglish,
            SessionId = batch.SessionId,
            SessionName = batch.Session.Name,
            TimingId = batch.TimingId,
            TimingName = batch.Timing != null ? $"{batch.Timing.StartTime:HH:mm} - {batch.Timing.EndTime:HH:mm}" : "Not Assigned",
            RoomId = batch.RoomId,
            RoomName = batch.Room != null ? (batch.Room.RoomName ?? batch.Room.RoomNumber) : "Not Assigned",
            PrimaryInstructorId = batch.PrimaryInstructorId,
            PrimaryInstructorName = batch.PrimaryInstructor != null ? $"{batch.PrimaryInstructor.FirstName} {batch.PrimaryInstructor.LastName}" : "Not Assigned",
            SecondaryInstructorId = batch.SecondaryInstructorId,
            SecondaryInstructorName = batch.SecondaryInstructor != null ? $"{batch.SecondaryInstructor.FirstName} {batch.SecondaryInstructor.LastName}" : "Not Assigned",
            StartDate = batch.StartDate,
            EndDate = batch.EndDate,
            MaxStudents = batch.MaxStudents,
            CurrentEnrollment = batch.Students.Count,
            Status = batch.Status,
            Notes = batch.Notes,
            CreatedDate = batch.CreatedDate,
            CreatedBy = batch.CreatedBy,
            ModifiedDate = batch.ModifiedDate,
            ModifiedBy = batch.ModifiedBy
        };

        return View(model);
    }

    // GET: Batch/Create
    public async Task<IActionResult> Create()
    {
        var model = new BatchViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6),
            Status = "Active"
        };

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // POST: Batch/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BatchViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if batch code is unique
            if (await _context.Batches.AnyAsync(b => b.BatchCode == model.BatchCode && !b.IsDeleted))
            {
                ModelState.AddModelError("BatchCode", "Batch code already exists.");
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            // Validate room capacity and timing conflicts
            var validationResult = await ValidateRoomAndTimingAsync(model);
            if (!validationResult.IsValid)
            {
                ModelState.AddModelError("", validationResult.ErrorMessage);
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            var batch = new Batch
            {
                BatchCode = model.BatchCode,
                BatchName = model.BatchName,
                TradeId = model.TradeId,
                SessionId = model.SessionId,
                TimingId = model.TimingId,
                RoomId = model.RoomId,
                PrimaryInstructorId = model.PrimaryInstructorId,
                SecondaryInstructorId = model.SecondaryInstructorId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxStudents = model.MaxStudents,
                Status = model.Status,
                Notes = model.Notes,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Batches.Add(batch);
            
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Batch created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("BatchCode") == true)
            {
                _logger.LogWarning(ex, "Attempted to create batch with duplicate BatchCode: {BatchCode}", model.BatchCode);
                ModelState.AddModelError("BatchCode", "This batch code is already in use. Please choose a different code.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating batch {BatchCode}", model.BatchCode);
                ModelState.AddModelError("", "An error occurred while creating the batch. Please try again.");
            }
        }

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // GET: Batch/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var batch = await _context.Batches
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (batch == null)
        {
            return NotFound();
        }

        var model = new BatchViewModel
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            TradeId = batch.TradeId,
            SessionId = batch.SessionId,
            TimingId = batch.TimingId,
            RoomId = batch.RoomId,
            PrimaryInstructorId = batch.PrimaryInstructorId,
            SecondaryInstructorId = batch.SecondaryInstructorId,
            StartDate = batch.StartDate,
            EndDate = batch.EndDate,
            MaxStudents = batch.MaxStudents,
            Status = batch.Status,
            Notes = batch.Notes
        };

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // POST: Batch/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BatchViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check if batch code is unique (excluding current batch)
            if (await _context.Batches.AnyAsync(b => b.BatchCode == model.BatchCode && b.Id != id && !b.IsDeleted))
            {
                ModelState.AddModelError("BatchCode", "Batch code already exists.");
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            // Validate room capacity and timing conflicts (excluding current batch)
            var validationResult = await ValidateRoomAndTimingAsync(model, id);
            if (!validationResult.IsValid)
            {
                ModelState.AddModelError("", validationResult.ErrorMessage);
                await PopulateSelectListsAsync(model);
                return View(model);
            }

            var batch = await _context.Batches.FindAsync(id);
            if (batch == null || batch.IsDeleted)
            {
                return NotFound();
            }

            batch.BatchCode = model.BatchCode;
            batch.BatchName = model.BatchName;
            batch.TradeId = model.TradeId;
            batch.SessionId = model.SessionId;
            batch.TimingId = model.TimingId;
            batch.RoomId = model.RoomId;
            batch.PrimaryInstructorId = model.PrimaryInstructorId;
            batch.SecondaryInstructorId = model.SecondaryInstructorId;
            batch.StartDate = model.StartDate;
            batch.EndDate = model.EndDate;
            batch.MaxStudents = model.MaxStudents;
            batch.Status = model.Status;
            batch.Notes = model.Notes;
            batch.ModifiedDate = DateTime.UtcNow;
            batch.ModifiedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Batch updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("BatchCode") == true)
            {
                _logger.LogWarning(ex, "Attempted to update batch with duplicate BatchCode: {BatchCode}", model.BatchCode);
                ModelState.AddModelError("BatchCode", "This batch code is already in use. Please choose a different code.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating batch {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the batch.");
            }
        }

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // GET: Batch/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.Trade)
            .Include(b => b.Session)
            .Include(b => b.Students.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (batch == null)
        {
            return NotFound();
        }

        var model = new BatchViewModel
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            TradeName = batch.Trade.NameEnglish,
            SessionName = batch.Session.Name,
            StartDate = batch.StartDate,
            EndDate = batch.EndDate,
            MaxStudents = batch.MaxStudents,
            CurrentEnrollment = batch.Students.Count,
            Status = batch.Status
        };

        return View(model);
    }

    // POST: Batch/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var batch = await _context.Batches.FindAsync(id);
        if (batch == null || batch.IsDeleted)
        {
            return NotFound();
        }

        // Check if batch has enrolled students
        var hasStudents = await _context.Students.AnyAsync(s => s.BatchId == id && !s.IsDeleted);
        if (hasStudents)
        {
            TempData["ErrorMessage"] = "Cannot delete batch because it has enrolled students.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        // Soft delete
        batch.IsDeleted = true;
        batch.ModifiedDate = DateTime.UtcNow;
        batch.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Batch deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync(BatchViewModel model)
    {
        // Get active trades
        var trades = await _context.Trades
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish })
            .ToListAsync();
        model.Trades = new SelectList(trades, "Id", "NameEnglish", model.TradeId);

        // Get sessions
        var sessions = await _context.Sessions
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();
        model.Sessions = new SelectList(sessions, "Id", "Name", model.SessionId);

        // Get timings
        var timings = await _context.Timings
            .Where(t => !t.IsDeleted)
            .Select(t => new { t.Id, Name = $"{t.StartTime:HH:mm} - {t.EndTime:HH:mm}" })
            .ToListAsync();
        model.Timings = new SelectList(timings, "Id", "Name", model.TimingId);

        // Get rooms
        var rooms = await _context.Rooms
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Id, Name = r.RoomName ?? r.RoomNumber })
            .ToListAsync();
        model.Rooms = new SelectList(rooms, "Id", "Name", model.RoomId);

        // Get active teachers
        var teachers = await _context.Teachers
            .Where(t => t.Status == "Active" && !t.IsDeleted)
            .Select(t => new { t.Id, Name = $"{t.FirstName} {t.LastName} ({t.TeacherCode})" })
            .ToListAsync();
        model.PrimaryInstructors = new SelectList(teachers, "Id", "Name", model.PrimaryInstructorId);
        model.SecondaryInstructors = new SelectList(teachers, "Id", "Name", model.SecondaryInstructorId);

        // Status options
        model.StatusOptions = new SelectList(new[]
        {
            new { Value = "Active", Text = "Active" },
            new { Value = "Completed", Text = "Completed" },
            new { Value = "Cancelled", Text = "Cancelled" }
        }, "Value", "Text", model.Status);
    }

    private async Task<ValidationResult> ValidateRoomAndTimingAsync(BatchViewModel model, int? excludeBatchId = null)
    {
        // Skip validation if room or timing is not selected
        if (!model.RoomId.HasValue || !model.TimingId.HasValue)
        {
            return new ValidationResult { IsValid = true };
        }

        // Get room capacity
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == model.RoomId.Value && !r.IsDeleted);
        if (room == null)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Selected room not found." };
        }

        // Check if batch max students exceeds room capacity
        if (model.MaxStudents > room.Capacity)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = $"Batch capacity ({model.MaxStudents}) cannot exceed room capacity ({room.Capacity})." 
            };
        }

        // Check for room-timing conflicts (double booking)
        var query = _context.Batches
            .Where(b => b.RoomId == model.RoomId.Value && 
                       b.TimingId == model.TimingId.Value && 
                       !b.IsDeleted &&
                       b.Status == "Active");

        if (excludeBatchId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBatchId.Value);
        }

        var conflictingBatch = await query.FirstOrDefaultAsync();
        if (conflictingBatch != null)
        {
            var timing = await _context.Timings.FirstOrDefaultAsync(t => t.Id == model.TimingId.Value);
            return new ValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = $"Room {room.RoomName ?? room.RoomNumber} is already booked for timing {timing?.StartTime:HH:mm} - {timing?.EndTime:HH:mm} by batch {conflictingBatch.BatchCode}." 
            };
        }

        return new ValidationResult { IsValid = true };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
