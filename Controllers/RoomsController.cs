using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class RoomsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(ApplicationDbContext context, ILogger<RoomsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Rooms
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", string typeFilter = "")
    {
        const int pageSize = 15;
        
        var query = _context.Rooms
            .Where(r => !r.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.RoomNumber.Contains(searchTerm) ||
                                   (r.RoomName != null && r.RoomName.Contains(searchTerm)) ||
                                   (r.Building != null && r.Building.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(r => r.RoomType == typeFilter);
        }

        var totalRecords = await query.CountAsync();

        var rooms = await query
            .OrderBy(r => r.RoomNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoomViewModel
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                RoomName = r.RoomName ?? string.Empty,
                Capacity = r.Capacity,
                RoomType = r.RoomType ?? string.Empty,
                Building = r.Building ?? string.Empty,
                Floor = r.Floor ?? string.Empty,
                Equipment = r.Equipment ?? string.Empty,
                HasProjector = r.HasProjector,
                HasComputers = r.HasComputers,
                HasAirConditioning = r.HasAirConditioning,
                CreatedDate = r.CreatedDate,
                CreatedBy = r.CreatedBy ?? string.Empty,
                CurrentBatches = _context.Batches.Count(b => b.RoomId == r.Id && !b.IsDeleted && b.Status == "Active")
            })
            .ToListAsync();

        var model = new RoomListViewModel
        {
            Rooms = rooms,
            SearchTerm = searchTerm,
            TypeFilter = typeFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            RoomTypes = await _context.Rooms
                .Where(r => !r.IsDeleted)
                .Select(r => r.RoomType)
                .Distinct()
                .ToListAsync()
        };

        return View(model);
    }

    // GET: Rooms/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var room = await _context.Rooms
            .Include(r => r.Batches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Trade)
            .Include(r => r.Batches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Session)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        var model = new RoomViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomName = room.RoomName ?? string.Empty,
            Capacity = room.Capacity,
            RoomType = room.RoomType ?? string.Empty,
            Building = room.Building ?? string.Empty,
            Floor = room.Floor ?? string.Empty,
            Equipment = room.Equipment ?? string.Empty,
            HasProjector = room.HasProjector,
            HasComputers = room.HasComputers,
            HasAirConditioning = room.HasAirConditioning,
            CreatedDate = room.CreatedDate,
            CreatedBy = room.CreatedBy ?? string.Empty,
            ModifiedDate = room.ModifiedDate,
            ModifiedBy = room.ModifiedBy ?? string.Empty,
            CurrentBatches = room.Batches.Count(b => b.Status == "Active"),
            AssignedBatches = room.Batches
                .Where(b => b.Status == "Active")
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

    // GET: Rooms/Create
    public IActionResult Create()
    {
        var model = new RoomViewModel();
        PopulateRoomTypes(model);
        return View(model);
    }

    // POST: Rooms/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if room number is unique
            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == model.RoomNumber && !r.IsDeleted))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists.");
                PopulateRoomTypes(model);
                return View(model);
            }

            var room = new Room
            {
                RoomNumber = model.RoomNumber,
                RoomName = model.RoomName,
                Capacity = model.Capacity,
                RoomType = model.RoomType,
                Building = model.Building,
                Floor = model.Floor,
                Equipment = model.Equipment,
                HasProjector = model.HasProjector,
                HasComputers = model.HasComputers,
                HasAirConditioning = model.HasAirConditioning,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Room created successfully!";
            return RedirectToAction(nameof(Index));
        }

        PopulateRoomTypes(model);
        return View(model);
    }

    // GET: Rooms/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        var model = new RoomViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomName = room.RoomName ?? string.Empty,
            Capacity = room.Capacity,
            RoomType = room.RoomType ?? string.Empty,
            Building = room.Building ?? string.Empty,
            Floor = room.Floor ?? string.Empty,
            Equipment = room.Equipment ?? string.Empty,
            HasProjector = room.HasProjector,
            HasComputers = room.HasComputers,
            HasAirConditioning = room.HasAirConditioning
        };

        PopulateRoomTypes(model);
        return View(model);
    }

    // POST: Rooms/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check if room number is unique (excluding current room)
            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == model.RoomNumber && r.Id != id && !r.IsDeleted))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists.");
                PopulateRoomTypes(model);
                return View(model);
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null || room.IsDeleted)
            {
                return NotFound();
            }

            room.RoomNumber = model.RoomNumber;
            room.RoomName = model.RoomName;
            room.Capacity = model.Capacity;
            room.RoomType = model.RoomType;
            room.Building = model.Building;
            room.Floor = model.Floor;
            room.Equipment = model.Equipment;
            room.HasProjector = model.HasProjector;
            room.HasComputers = model.HasComputers;
            room.HasAirConditioning = model.HasAirConditioning;
            room.ModifiedDate = DateTime.UtcNow;
            room.ModifiedBy = User.Identity?.Name ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Room updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the room.");
            }
        }

        PopulateRoomTypes(model);
        return View(model);
    }

    // GET: Rooms/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _context.Rooms
            .Include(r => r.Batches.Where(b => !b.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        var model = new RoomViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomName = room.RoomName ?? string.Empty,
            Capacity = room.Capacity,
            RoomType = room.RoomType ?? string.Empty,
            Building = room.Building ?? string.Empty,
            Floor = room.Floor ?? string.Empty,
            HasProjector = room.HasProjector,
            HasComputers = room.HasComputers,
            HasAirConditioning = room.HasAirConditioning,
            CurrentBatches = room.Batches.Count(b => b.Status == "Active")
        };

        return View(model);
    }

    // POST: Rooms/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null || room.IsDeleted)
        {
            return NotFound();
        }

        // Check if room has active batches
        var hasActiveBatches = await _context.Batches.AnyAsync(b => b.RoomId == id && !b.IsDeleted && b.Status == "Active");
        if (hasActiveBatches)
        {
            TempData["ErrorMessage"] = "Cannot delete room because it has active batches assigned.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        // Soft delete
        room.IsDeleted = true;
        room.ModifiedDate = DateTime.UtcNow;
        room.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Room deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateRoomTypes(RoomViewModel model)
    {
        var roomTypes = new[]
        {
            "Classroom",
            "Laboratory",
            "Workshop",
            "Computer Lab",
            "Library",
            "Conference Room",
            "Auditorium",
            "Office",
            "Other"
        };

        ViewBag.RoomTypes = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            roomTypes.Select(rt => new { Value = rt, Text = rt }),
            "Value", "Text");
    }
}