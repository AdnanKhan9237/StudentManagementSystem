using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class TimingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimingsController> _logger;

    public TimingsController(ApplicationDbContext context, ILogger<TimingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Timings
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", string typeFilter = "")
    {
        const int pageSize = 15;
        
        var query = _context.Timings
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => t.Name.Contains(searchTerm) ||
                                   t.Shift.Contains(searchTerm) ||
                                   (t.Type != null && t.Type.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(t => t.Type == typeFilter);
        }

        var totalRecords = await query.CountAsync();

        var timings = await query
            .OrderBy(t => t.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TimingViewModel
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Shift = t.Shift,
                Type = t.Type,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                CreatedBy = t.CreatedBy,
                BatchCount = _context.Batches.Count(b => b.TimingId == t.Id && !b.IsDeleted)
            })
            .ToListAsync();

        var model = new TimingListViewModel
        {
            Timings = timings,
            SearchTerm = searchTerm,
            TypeFilter = typeFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TimingTypes = await _context.Timings
                .Where(t => !t.IsDeleted && t.Type != null)
                .Select(t => t.Type)
                .Distinct()
                .ToListAsync()
        };

        return View(model);
    }

    // GET: Timings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var timing = await _context.Timings
            .Include(t => t.Batches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Trade)
            .Include(t => t.Batches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Session)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (timing == null)
        {
            return NotFound();
        }

        var model = new TimingViewModel
        {
            Id = timing.Id,
            Name = timing.Name,
            StartTime = timing.StartTime,
            EndTime = timing.EndTime,
            Shift = timing.Shift,
            Type = timing.Type,
            Description = timing.Description,
            IsActive = timing.IsActive,
            CreatedDate = timing.CreatedDate,
            CreatedBy = timing.CreatedBy,
            ModifiedDate = timing.ModifiedDate,
            ModifiedBy = timing.ModifiedBy,
            BatchCount = timing.Batches.Count,
            AssignedBatches = timing.Batches
                .Select(b => new BatchSummaryViewModel
                {
                    Id = b.Id,
                    BatchCode = b.BatchCode,
                    BatchName = b.BatchName,
                    TradeName = b.Trade.NameEnglish,
                    SessionName = b.Session.Name,
                    StudentCount = b.Students.Count(s => !s.IsDeleted)
                }).ToList()
        };

        return View(model);
    }

    // GET: Timings/Create
    public IActionResult Create()
    {
        var model = new TimingViewModel
        {
            IsActive = true
        };
        PopulateTimingTypes(model);
        PopulateShifts(model);
        return View(model);
    }

    // POST: Timings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimingViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Validate time range
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                PopulateTimingTypes(model);
                PopulateShifts(model);
                return View(model);
            }

            // Removed overlap validation - allow any timing combinations

            var timing = new Timing
            {
                Name = model.Name,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Shift = model.Shift,
                Type = model.Type,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Timings.Add(timing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Timing created successfully!";
            return RedirectToAction(nameof(Index));
        }

        PopulateTimingTypes(model);
        PopulateShifts(model);
        return View(model);
    }

    // GET: Timings/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var timing = await _context.Timings
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (timing == null)
        {
            return NotFound();
        }

        var model = new TimingViewModel
        {
            Id = timing.Id,
            Name = timing.Name,
            StartTime = timing.StartTime,
            EndTime = timing.EndTime,
            Shift = timing.Shift,
            Type = timing.Type,
            Description = timing.Description,
            IsActive = timing.IsActive
        };

        PopulateTimingTypes(model);
        PopulateShifts(model);
        return View(model);
    }

    // POST: Timings/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TimingViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Validate time range
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                PopulateTimingTypes(model);
                PopulateShifts(model);
                return View(model);
            }

            // Removed overlap validation - allow any timing combinations

            var timing = await _context.Timings.FindAsync(id);
            if (timing == null || timing.IsDeleted)
            {
                return NotFound();
            }

            timing.Name = model.Name;
            timing.StartTime = model.StartTime;
            timing.EndTime = model.EndTime;
            timing.Shift = model.Shift;
            timing.Type = model.Type;
            timing.Description = model.Description;
            timing.IsActive = model.IsActive;
            timing.ModifiedDate = DateTime.UtcNow;
            timing.ModifiedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Timing updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timing {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the timing.");
            }
        }

        PopulateTimingTypes(model);
        PopulateShifts(model);
        return View(model);
    }

    // GET: Timings/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var timing = await _context.Timings
            .Include(t => t.Batches.Where(b => !b.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (timing == null)
        {
            return NotFound();
        }

        var model = new TimingViewModel
        {
            Id = timing.Id,
            Name = timing.Name,
            StartTime = timing.StartTime,
            EndTime = timing.EndTime,
            Shift = timing.Shift,
            Type = timing.Type,
            Description = timing.Description,
            BatchCount = timing.Batches.Count
        };

        return View(model);
    }

    // POST: Timings/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var timing = await _context.Timings.FindAsync(id);
        if (timing == null || timing.IsDeleted)
        {
            return NotFound();
        }

        // Check if timing has active batches
        var hasActiveBatches = await _context.Batches.AnyAsync(b => b.TimingId == id && !b.IsDeleted);
        if (hasActiveBatches)
        {
            TempData["ErrorMessage"] = "Cannot delete timing because it has active batches assigned.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        // Soft delete
        timing.IsDeleted = true;
        timing.ModifiedDate = DateTime.UtcNow;
        timing.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Timing deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateTimingTypes(TimingViewModel model)
    {
        var types = new[]
        {
            "Morning",
            "Afternoon", 
            "Evening",
            "Night",
            "Weekend"
        };

        ViewBag.TimingTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            types.Select(t => new { Value = t, Text = t }),
            "Value", "Text", model.Type);
    }

    private void PopulateShifts(TimingViewModel model)
    {
        var shifts = new[]
        {
            "Morning",
            "Evening",
            "Night"
        };

        ViewBag.Shifts = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            shifts.Select(s => new { Value = s, Text = s }),
            "Value", "Text", model.Shift);
    }
}