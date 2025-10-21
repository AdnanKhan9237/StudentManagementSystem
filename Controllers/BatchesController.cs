using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class BatchesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BatchesController> _logger;

    public BatchesController(ApplicationDbContext context, ILogger<BatchesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Batches
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? sessionId = null, int? tradeId = null, string status = "")
    {
        const int pageSize = 15;
        
        var query = _context.Batches
            .Include(b => b.Session)
            .Include(b => b.Trade)
            .Include(b => b.Room)
            .Include(b => b.Timing)
            .Where(b => !b.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b => b.BatchCode.Contains(searchTerm) ||
                                   b.BatchName.Contains(searchTerm) ||
                                   b.Trade.NameEnglish.Contains(searchTerm) ||
                                   b.Session.Name.Contains(searchTerm));
        }

        if (sessionId.HasValue)
        {
            query = query.Where(b => b.SessionId == sessionId);
        }

        if (tradeId.HasValue)
        {
            query = query.Where(b => b.TradeId == tradeId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var totalRecords = await query.CountAsync();

        var batches = await query
            .OrderByDescending(b => b.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BatchViewModel
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                SessionName = b.Session.Name,
                TradeName = b.Trade.NameEnglish,
                RoomNumber = b.Room != null ? b.Room.RoomNumber : "Not Assigned",
                TimingDescription = b.Timing != null ? $"{b.Timing.StartTime:hh\\:mm} - {b.Timing.EndTime:hh\\:mm} ({b.Timing.Name})" : "Not Set",
                Status = b.Status,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                MaxStudents = b.MaxStudents,
                CurrentStudents = b.Students.Count(s => !s.IsDeleted),
                CreatedDate = b.CreatedDate,
                CreatedBy = b.CreatedBy
            })
            .ToListAsync();

        var model = new BatchListViewModel
        {
            Batches = batches,
            SearchTerm = searchTerm,
            SessionId = sessionId,
            TradeId = tradeId,
            Status = status,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            Sessions = (await _context.Sessions
                .Where(s => !s.IsDeleted)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync()).Cast<dynamic>().ToList(),
            Trades = (await _context.Trades
                .Where(t => !t.IsDeleted)
                .Select(t => new { t.Id, t.NameEnglish })
                .ToListAsync()).Cast<dynamic>().ToList(),
            Statuses = new[] { "Active", "Inactive", "Completed", "Suspended" }
        };

        return View(model);
    }

    // GET: Batches/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.Session)
            .Include(b => b.Trade)
            .Include(b => b.Room)
            .Include(b => b.Timing)
            .Include(b => b.Students.Where(s => !s.IsDeleted))
            .ThenInclude(s => s.Student)
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
            SessionId = batch.SessionId,
            SessionName = batch.Session.Name,
            TradeId = batch.TradeId,
            TradeName = batch.Trade.NameEnglish,
            RoomId = batch.RoomId,
            RoomNumber = batch.Room?.RoomNumber ?? "Not Assigned",
            TimingId = batch.TimingId,
            TimingDescription = batch.Timing != null ? $"{batch.Timing.StartTime:hh\\:mm} - {batch.Timing.EndTime:hh\\:mm} ({batch.Timing.Name})" : "Not Set",
            Status = batch.Status,
            StartDate = batch.StartDate,
            EndDate = batch.EndDate,
            MaxStudents = batch.MaxStudents,
            CurrentStudents = batch.Students.Count,
            Description = batch.Description,
            CreatedDate = batch.CreatedDate,
            CreatedBy = batch.CreatedBy,
            ModifiedDate = batch.ModifiedDate,
            ModifiedBy = batch.ModifiedBy,
            EnrolledStudents = batch.Students
                .Select(bs => new StudentSummaryViewModel
                {
                    Id = bs.Student.Id,
                    StudentCode = bs.Student.StudentCode,
                    FullName = $"{bs.Student.FirstName} {bs.Student.LastName}",
                    Email = bs.Student.Email,
                    EnrollmentDate = bs.EnrollmentDate
                })
                .OrderBy(s => s.FullName)
                .ToList()
        };

        return View(model);
    }

    // GET: Batches/Create
    public async Task<IActionResult> Create()
    {
        var model = new BatchViewModel();
        await PopulateDropDowns(model);
        return View(model);
    }

    // POST: Batches/Create
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
                await PopulateDropDowns(model);
                return View(model);
            }

            // Check student limit (max 50 per batch)
            if (model.MaxStudents > 50)
            {
                ModelState.AddModelError("MaxStudents", "Maximum students per batch cannot exceed 50.");
                await PopulateDropDowns(model);
                return View(model);
            }

            // Check room capacity if room is assigned
            if (model.RoomId.HasValue && model.RoomId > 0 && model.MaxStudents > 0)
            {
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room != null && !room.IsDeleted && model.MaxStudents > room.Capacity)
                {
                    ModelState.AddModelError("MaxStudents", $"Maximum students cannot exceed room capacity ({room.Capacity}).");
                    await PopulateDropDowns(model);
                    return View(model);
                }
            }

            var batch = new Batch
            {
                BatchCode = model.BatchCode,
                BatchName = model.BatchName,
                SessionId = model.SessionId,
                TradeId = model.TradeId,
                RoomId = model.RoomId,
                TimingId = model.TimingId,
                Status = model.Status,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxStudents = model.MaxStudents,
                Description = model.Description,
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

        await PopulateDropDowns(model);
        return View(model);
    }

    // GET: Batches/Edit/5
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
            SessionId = batch.SessionId,
            TradeId = batch.TradeId,
            RoomId = batch.RoomId,
            TimingId = batch.TimingId,
            Status = batch.Status,
            StartDate = batch.StartDate,
            EndDate = batch.EndDate,
            MaxStudents = batch.MaxStudents,
            Description = batch.Description
        };

        await PopulateDropDowns(model);
        return View(model);
    }

    // POST: Batches/Edit/5
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
                await PopulateDropDowns(model);
                return View(model);
            }

            // Check student limit (max 50 per batch)
            if (model.MaxStudents > 50)
            {
                ModelState.AddModelError("MaxStudents", "Maximum students per batch cannot exceed 50.");
                await PopulateDropDowns(model);
                return View(model);
            }

            // Check room capacity if room is assigned
            if (model.RoomId.HasValue && model.RoomId > 0 && model.MaxStudents > 0)
            {
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room != null && !room.IsDeleted && model.MaxStudents > room.Capacity)
                {
                    ModelState.AddModelError("MaxStudents", $"Maximum students cannot exceed room capacity ({room.Capacity}).");
                    await PopulateDropDowns(model);
                    return View(model);
                }
            }

            var batch = await _context.Batches.FindAsync(id);
            if (batch == null || batch.IsDeleted)
            {
                return NotFound();
            }

            batch.BatchCode = model.BatchCode;
            batch.BatchName = model.BatchName;
            batch.SessionId = model.SessionId;
            batch.TradeId = model.TradeId;
            batch.RoomId = model.RoomId;
            batch.TimingId = model.TimingId;
            batch.Status = model.Status;
            batch.StartDate = model.StartDate;
            batch.EndDate = model.EndDate;
            batch.MaxStudents = model.MaxStudents;
            batch.Description = model.Description;
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

        await PopulateDropDowns(model);
        return View(model);
    }

    // GET: Batches/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.Session)
            .Include(b => b.Trade)
            .Include(b => b.Room)
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
            SessionName = batch.Session.Name,
            TradeName = batch.Trade.NameEnglish,
            RoomNumber = batch.Room?.RoomNumber ?? "Not Assigned",
            Status = batch.Status,
            CurrentStudents = batch.Students.Count
        };

        return View(model);
    }

    // POST: Batches/Delete/5
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
        var hasStudents = await _context.BatchStudents.AnyAsync(bs => bs.BatchId == id && !bs.IsDeleted);
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

    private async Task PopulateDropDowns(BatchViewModel model)
    {
        ViewBag.Sessions = new SelectList(
            await _context.Sessions.Where(s => !s.IsDeleted).ToListAsync(),
            "Id", "Name", model.SessionId);

        ViewBag.Trades = new SelectList(
            await _context.Trades.Where(t => !t.IsDeleted).ToListAsync(),
            "Id", "NameEnglish", model.TradeId);

        ViewBag.Rooms = new SelectList(
            await _context.Rooms.Where(r => !r.IsDeleted)
                .Select(r => new { r.Id, DisplayText = $"{r.RoomNumber} - {r.RoomName} ({r.Capacity} capacity)" })
                .ToListAsync(),
            "Id", "DisplayText", model.RoomId);

        var timings = await _context.Timings.Where(t => !t.IsDeleted).ToListAsync();
        ViewBag.Timings = new SelectList(
            timings.Select(t => new { t.Id, DisplayText = $"{t.StartTime:hh\\:mm} - {t.EndTime:hh\\:mm} ({t.Name})" }),
            "Id", "DisplayText", model.TimingId);

        ViewBag.Statuses = new SelectList(new[]
        {
            new { Value = "Active", Text = "Active" },
            new { Value = "Inactive", Text = "Inactive" },
            new { Value = "Completed", Text = "Completed" },
            new { Value = "Suspended", Text = "Suspended" }
        }, "Value", "Text", model.Status);
    }
}