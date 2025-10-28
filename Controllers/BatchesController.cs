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
            .Include(b => b.PrimaryInstructor)
            .Include(b => b.SecondaryInstructor)
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

        var batchList = await query
            .OrderByDescending(b => b.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        var batches = new List<BatchViewModel>();
        foreach (var b in batchList)
        {
            var studentCount = await _context.Students.CountAsync(s => s.BatchId == b.Id && !s.IsDeleted);
            
            batches.Add(new BatchViewModel
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                SessionName = b.Session.Name,
                TradeName = b.Trade.NameEnglish,
                RoomNumber = b.Room != null ? b.Room.RoomNumber : "Not Assigned",
                TimingDescription = b.Timing != null ? $"{b.Timing.StartTime:hh\\:mm} - {b.Timing.EndTime:hh\\:mm} ({b.Timing.Name})" : "Not Set",
                PrimaryInstructorName = b.PrimaryInstructor != null ? $"{b.PrimaryInstructor.FirstName} {b.PrimaryInstructor.LastName}" : "Not Assigned",
                SecondaryInstructorName = b.SecondaryInstructor != null ? $"{b.SecondaryInstructor.FirstName} {b.SecondaryInstructor.LastName}" : "Not Assigned",
                Status = b.Status,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                MaxStudents = b.MaxStudents,
                CurrentStudents = studentCount,
                CreatedDate = b.CreatedDate,
                CreatedBy = b.CreatedBy
            });
        }

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
                    Email = bs.Student.Email ?? string.Empty,
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
        _logger.LogInformation("=== BATCH CREATE DEBUG START ===");
        _logger.LogInformation("SelectedTimingIds: {Count}", model.SelectedTimingIds?.Count ?? 0);
        
        // Calculate max students from selected timings BEFORE validation
        int calculatedMaxStudents = 0;
        if (model.SelectedTimingIds != null && model.SelectedTimingIds.Any())
        {
            foreach (var timingId in model.SelectedTimingIds)
            {
                var maxStudentsKey = $"TimingMaxStudents_{timingId}";
                if (Request.Form.ContainsKey(maxStudentsKey))
                {
                    if (int.TryParse(Request.Form[maxStudentsKey], out int timingMaxStudents))
                    {
                        calculatedMaxStudents += timingMaxStudents;
                        _logger.LogInformation("Timing {TimingId}: {MaxStudents}", timingId, timingMaxStudents);
                    }
                }
            }
            model.MaxStudents = calculatedMaxStudents;
        }
        
        _logger.LogInformation("Calculated MaxStudents: {MaxStudents}", model.MaxStudents);
        
        // Remove MaxStudents validation error since we calculate it
        ModelState.Remove("MaxStudents");
        
        // Log ModelState errors
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is INVALID");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors != null && errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("{Key}: {Error}", key, error.ErrorMessage);
                    }
                }
            }
        }
        else
        {
            _logger.LogInformation("ModelState is VALID");
        }
        
        if (ModelState.IsValid)
        {
            // Check if batch code is unique
            if (await _context.Batches.AnyAsync(b => b.BatchCode == model.BatchCode && !b.IsDeleted))
            {
                ModelState.AddModelError("BatchCode", "Batch code already exists.");
                await PopulateDropDowns(model);
                return View(model);
            }
            
            // Validate that max students is set
            if (model.MaxStudents <= 0)
            {
                ModelState.AddModelError("MaxStudents", "Please select at least one timing with max students greater than 0.");
                await PopulateDropDowns(model);
                return View(model);
            }

            // Check room capacity per timing if room is assigned
            if (model.RoomId.HasValue && model.RoomId > 0)
            {
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room != null && !room.IsDeleted)
                {
                    // Check each timing's max students against room capacity
                    // Since the same room can be used for different timings, we check per timing
                    if (model.SelectedTimingIds != null && model.SelectedTimingIds.Any())
                    {
                        foreach (var timingId in model.SelectedTimingIds)
                        {
                            var maxStudentsKey = $"TimingMaxStudents_{timingId}";
                            if (Request.Form.ContainsKey(maxStudentsKey))
                            {
                                if (int.TryParse(Request.Form[maxStudentsKey], out int timingMaxStudents))
                                {
                                    if (timingMaxStudents > room.Capacity)
                                    {
                                        var timing = await _context.Timings.FindAsync(timingId);
                                        ModelState.AddModelError("", $"Timing '{timing?.Name}' max students ({timingMaxStudents}) cannot exceed room capacity ({room.Capacity}).");
                                        await PopulateDropDowns(model);
                                        return View(model);
                                    }
                                }
                            }
                        }
                    }
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
                
                // Create BatchTiming records for selected timings
                if (model.SelectedTimingIds != null && model.SelectedTimingIds.Any())
                {
                    foreach (var timingId in model.SelectedTimingIds)
                    {
                        // Get max students for this timing from form data
                        var maxStudentsKey = $"TimingMaxStudents_{timingId}";
                        int timingMaxStudents = model.MaxStudents; // Default to batch max
                        
                        if (Request.Form.ContainsKey(maxStudentsKey))
                        {
                            int.TryParse(Request.Form[maxStudentsKey], out timingMaxStudents);
                        }
                        
                        var batchTiming = new BatchTiming
                        {
                            BatchId = batch.Id,
                            TimingId = timingId,
                            MaxStudents = timingMaxStudents,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "System"
                        };
                        
                        _context.BatchTimings.Add(batchTiming);
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
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

    // GET: Batches/AssignTeacher/5 - Assign teachers to a batch
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
    public async Task<IActionResult> AssignTeacher(int id)
    {
        var batch = await _context.Batches
            .Include(b => b.Session)
            .Include(b => b.Trade)
            .Include(b => b.Room)
            .Include(b => b.Timing)
            .Include(b => b.PrimaryInstructor)
            .Include(b => b.SecondaryInstructor)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (batch == null)
        {
            return NotFound();
        }

        var enrolledStudents = await _context.Students.CountAsync(s => s.BatchId == id && !s.IsDeleted);

        var model = new BatchViewModel
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            SessionName = batch.Session.Name,
            TradeName = batch.Trade.NameEnglish,
            RoomName = batch.Room?.RoomNumber ?? "Not assigned",
            TimingName = batch.Timing?.Name ?? "Not assigned",
            Status = batch.Status,
            MaxStudents = batch.MaxStudents,
            CurrentEnrollment = enrolledStudents,
            PrimaryInstructorId = batch.PrimaryInstructorId,
            PrimaryInstructorName = batch.PrimaryInstructor != null ? $"{batch.PrimaryInstructor.FirstName} {batch.PrimaryInstructor.LastName}" : "Not Assigned",
            SecondaryInstructorId = batch.SecondaryInstructorId,
            SecondaryInstructorName = batch.SecondaryInstructor != null ? $"{batch.SecondaryInstructor.FirstName} {batch.SecondaryInstructor.LastName}" : "Not Assigned"
        };

        return View(model);
    }

    // AJAX: Get available teachers
    [HttpGet]
    public async Task<IActionResult> GetAvailableTeachers()
    {
        try
        {
            var teachers = await _context.Teachers
                .Where(t => t.Status == "Active" && !t.IsDeleted)
                .Select(t => new {
                    id = t.Id,
                    name = t.FirstName + " " + t.LastName,
                    teacherCode = t.TeacherCode
                })
                .OrderBy(t => t.name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} available teachers", teachers.Count);
            return Json(teachers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available teachers");
            return Json(new List<object>());
        }
    }

    // AJAX: Assign teachers to batch
    [HttpPost]
    public async Task<IActionResult> AssignTeachers(int batchId, int primaryTeacherId, int? secondaryTeacherId)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch == null || batch.IsDeleted)
            {
                return Json(new { success = false, message = "Batch not found." });
            }

            // Verify teachers exist and are active
            var primaryTeacher = await _context.Teachers.FindAsync(primaryTeacherId);
            if (primaryTeacher == null || primaryTeacher.IsDeleted || primaryTeacher.Status != "Active")
            {
                return Json(new { success = false, message = "Primary teacher not found or inactive." });
            }

            if (secondaryTeacherId.HasValue)
            {
                var secondaryTeacher = await _context.Teachers.FindAsync(secondaryTeacherId.Value);
                if (secondaryTeacher == null || secondaryTeacher.IsDeleted || secondaryTeacher.Status != "Active")
                {
                    return Json(new { success = false, message = "Secondary teacher not found or inactive." });
                }
            }

            // Update batch assignments
            batch.PrimaryInstructorId = primaryTeacherId;
            batch.SecondaryInstructorId = secondaryTeacherId;
            batch.ModifiedDate = DateTime.UtcNow;
            batch.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            // Count affected students
            var studentCount = await _context.Students.CountAsync(s => s.BatchId == batchId && !s.IsDeleted);

            var secondaryTeacherName = "None";
            if (secondaryTeacherId.HasValue)
            {
                var secondaryTeacher = await _context.Teachers.FindAsync(secondaryTeacherId.Value);
                secondaryTeacherName = secondaryTeacher != null ? $"{secondaryTeacher.FirstName} {secondaryTeacher.LastName}" : "Unknown";
            }
            
            _logger.LogInformation("Teachers assigned to batch {BatchCode}: Primary={PrimaryTeacher}, Secondary={SecondaryTeacher}, Students affected={StudentCount}", 
                batch.BatchCode, $"{primaryTeacher.FirstName} {primaryTeacher.LastName}", 
                secondaryTeacherName, studentCount);

            return Json(new { 
                success = true, 
                message = $"Teachers assigned successfully! {studentCount} students are now assigned to the selected teachers." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning teachers to batch {BatchId}", batchId);
            return Json(new { success = false, message = "An error occurred while assigning teachers." });
        }
    }

    // AJAX: Clear teacher assignments from batch
    [HttpPost]
    public async Task<IActionResult> ClearTeacherAssignments(int batchId)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch == null || batch.IsDeleted)
            {
                return Json(new { success = false, message = "Batch not found." });
            }

            batch.PrimaryInstructorId = null;
            batch.SecondaryInstructorId = null;
            batch.ModifiedDate = DateTime.UtcNow;
            batch.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            var studentCount = await _context.Students.CountAsync(s => s.BatchId == batchId && !s.IsDeleted);

            _logger.LogInformation("Teacher assignments cleared from batch {BatchCode}, {StudentCount} students affected", 
                batch.BatchCode, studentCount);

            return Json(new { 
                success = true, 
                message = $"Teacher assignments cleared successfully! {studentCount} students are no longer assigned to teachers through this batch." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing teacher assignments from batch {BatchId}", batchId);
            return Json(new { success = false, message = "An error occurred while clearing assignments." });
        }
    }

    // AJAX: Get students in batch
    [HttpGet]
    public async Task<IActionResult> GetBatchStudents(int batchId)
    {
        try
        {
            var students = await _context.Students
                .Where(s => s.BatchId == batchId && !s.IsDeleted)
                .Select(s => new {
                    registrationNumber = s.RegistrationNumber,
                    fullName = s.FirstName + " " + s.LastName,
                    status = s.Status,
                    email = s.Email ?? "",
                    phone = s.PhoneNumber ?? ""
                })
                .OrderBy(s => s.fullName)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} students for batch {BatchId}", students.Count, batchId);
            return Json(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for batch {BatchId}", batchId);
            return Json(new List<object>());
        }
    }
}
