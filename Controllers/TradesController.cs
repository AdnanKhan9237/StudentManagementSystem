using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class TradesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TradesController> _logger;

    public TradesController(ApplicationDbContext context, ILogger<TradesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Trades
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", bool? activeFilter = null)
    {
        const int pageSize = 10;
        
        var query = _context.Trades
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => t.NameEnglish.Contains(searchTerm) ||
                                   t.NameUrdu.Contains(searchTerm) ||
                                   t.Code.Contains(searchTerm));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(t => t.IsActive == activeFilter.Value);
        }

        var totalRecords = await query.CountAsync();

        var trades = await query
            .OrderBy(t => t.NameEnglish)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TradeViewModel
            {
                Id = t.Id,
                NameEnglish = t.NameEnglish,
                NameUrdu = t.NameUrdu,
                Code = t.Code,
                DescriptionEnglish = t.DescriptionEnglish,
                DescriptionUrdu = t.DescriptionUrdu,
                Duration = t.Duration,
                TotalFee = t.TotalFee,
                MaxStudents = t.MaxStudents,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                CreatedBy = t.CreatedBy,
                CurrentStudents = _context.Students.Count(s => s.TradeId == t.Id && !s.IsDeleted)
            })
            .ToListAsync();

        var model = new TradeListViewModel
        {
            Trades = trades,
            SearchTerm = searchTerm,
            ActiveFilter = activeFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };

        return View(model);
    }

    // GET: Trades/Create
    public IActionResult Create()
    {
        var model = new TradeViewModel();
        return View(model);
    }

    // POST: Trades/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TradeViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if code is unique
            if (await _context.Trades.AnyAsync(t => t.Code == model.Code && !t.IsDeleted))
            {
                ModelState.AddModelError("Code", "Trade code already exists.");
                return View(model);
            }

            var trade = new Trade
            {
                NameEnglish = model.NameEnglish,
                NameUrdu = model.NameUrdu,
                Code = model.Code,
                DescriptionEnglish = model.DescriptionEnglish,
                DescriptionUrdu = model.DescriptionUrdu,
                Duration = model.Duration,
                TotalFee = model.TotalFee,
                MaxStudents = model.MaxStudents,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Trades.Add(trade);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trade created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Trades/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var trade = await _context.Trades
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TradeViewModel
            {
                Id = t.Id,
                NameEnglish = t.NameEnglish,
                NameUrdu = t.NameUrdu,
                Code = t.Code,
                DescriptionEnglish = t.DescriptionEnglish,
                DescriptionUrdu = t.DescriptionUrdu,
                Duration = t.Duration,
                TotalFee = t.TotalFee,
                MaxStudents = t.MaxStudents,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                CreatedBy = t.CreatedBy,
                ModifiedDate = t.ModifiedDate,
                ModifiedBy = t.ModifiedBy,
                CurrentStudents = _context.Students.Count(s => s.TradeId == t.Id && !s.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (trade == null)
        {
            return NotFound();
        }

        return View(trade);
    }

    // GET: Trades/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var trade = await _context.Trades
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TradeViewModel
            {
                Id = t.Id,
                NameEnglish = t.NameEnglish,
                NameUrdu = t.NameUrdu,
                Code = t.Code,
                DescriptionEnglish = t.DescriptionEnglish,
                DescriptionUrdu = t.DescriptionUrdu,
                Duration = t.Duration,
                TotalFee = t.TotalFee,
                MaxStudents = t.MaxStudents,
                IsActive = t.IsActive
            })
            .FirstOrDefaultAsync();

        if (trade == null)
        {
            return NotFound();
        }

        return View(trade);
    }

    // POST: Trades/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TradeViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check if code is unique (excluding current trade)
            if (await _context.Trades.AnyAsync(t => t.Code == model.Code && t.Id != id && !t.IsDeleted))
            {
                ModelState.AddModelError("Code", "Trade code already exists.");
                return View(model);
            }

            var trade = await _context.Trades.FindAsync(id);
            if (trade == null || trade.IsDeleted)
            {
                return NotFound();
            }

            trade.NameEnglish = model.NameEnglish;
            trade.NameUrdu = model.NameUrdu;
            trade.Code = model.Code;
            trade.DescriptionEnglish = model.DescriptionEnglish;
            trade.DescriptionUrdu = model.DescriptionUrdu;
            trade.Duration = model.Duration;
            trade.TotalFee = model.TotalFee;
            trade.MaxStudents = model.MaxStudents;
            trade.IsActive = model.IsActive;
            trade.ModifiedDate = DateTime.UtcNow;
            trade.ModifiedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trade updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trade {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the trade.");
            }
        }

        return View(model);
    }

    // GET: Trades/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var trade = await _context.Trades
            .Where(t => t.Id == id && !t.IsDeleted)
            .Select(t => new TradeViewModel
            {
                Id = t.Id,
                NameEnglish = t.NameEnglish,
                NameUrdu = t.NameUrdu,
                Code = t.Code,
                DescriptionEnglish = t.DescriptionEnglish,
                Duration = t.Duration,
                TotalFee = t.TotalFee,
                MaxStudents = t.MaxStudents,
                IsActive = t.IsActive,
                CurrentStudents = _context.Students.Count(s => s.TradeId == t.Id && !s.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (trade == null)
        {
            return NotFound();
        }

        return View(trade);
    }

    // POST: Trades/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trade = await _context.Trades.FindAsync(id);
        if (trade == null || trade.IsDeleted)
        {
            return NotFound();
        }

        // Check if trade has associated students
        var hasStudents = await _context.Students.AnyAsync(s => s.TradeId == id && !s.IsDeleted);
        if (hasStudents)
        {
            TempData["ErrorMessage"] = "Cannot delete trade because it has enrolled students.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        // Soft delete
        trade.IsDeleted = true;
        trade.ModifiedDate = DateTime.UtcNow;
        trade.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Trade deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
