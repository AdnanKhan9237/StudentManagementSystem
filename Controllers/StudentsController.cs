using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Interfaces;
using StudentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using StudentManagementSystem.Models.Entities;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers;

[Authorize]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IDropdownService _dropdownService;
    private readonly ILogger<StudentsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public StudentsController(IStudentService studentService, IDropdownService dropdownService, ILogger<StudentsController> logger, IWebHostEnvironment environment)
    {
        _studentService = studentService;
        _dropdownService = dropdownService;
        _logger = logger;
        _environment = environment;
    }

    // GET: Students - Accounts, Admin, SuperAdmin can see all; Teacher sees assigned; Student sees own profile only
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin},{UserRoles.Accounts},{UserRoles.Teacher}")]
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "")
    {
        StudentListViewModel model;
        
        if (User.IsInRole(UserRoles.Teacher))
        {
            // Teachers only see students assigned to their batches
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model = await _studentService.GetStudentsForTeacherAsync(teacherId!, page, 20, searchTerm, tradeFilter, sessionFilter, statusFilter);
        }
        else if (User.IsInRole(UserRoles.Student))
        {
            // Students should only see their own profile, redirect to profile page
            return RedirectToAction("Profile");
        }
        else
        {
            // SuperAdmin, Admin, Accounts see all students
            model = await _studentService.GetStudentsAsync(page, 20, searchTerm, tradeFilter, sessionFilter, statusFilter);
        }
        
        return View(model);
    }

    // GET: Students/Details/5 - All roles can view details (with restrictions applied in service layer)
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin},{UserRoles.Accounts},{UserRoles.Teacher},{UserRoles.Student}")]
    public async Task<IActionResult> Details(int id)
    {
        StudentViewModel? student;
        
        if (User.IsInRole(UserRoles.Teacher))
        {
            // Teachers can only view students assigned to their batches
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            student = await _studentService.GetStudentByIdForTeacherAsync(id, teacherId!);
        }
        else if (User.IsInRole(UserRoles.Student))
        {
            // Students can only view their own profile
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var studentProfile = await _studentService.GetStudentProfileAsync(userId!);
            if (studentProfile == null || studentProfile.Id != id)
            {
                return Forbid();
            }
            student = studentProfile;
        }
        else
        {
            // SuperAdmin, Admin, Accounts can view any student
            student = await _studentService.GetStudentByIdAsync(id);
        }
        
        if (student == null)
        {
            return NotFound();
        }
        
        return View(student);
    }

    // GET: Students/Create - Only Accounts role can create students
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts}")]
    public async Task<IActionResult> Create()
    {
        var model = new StudentViewModel();
        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // POST: Students/Create - Only Accounts role can create students
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts}")]
    public async Task<IActionResult> Create(StudentViewModel model)
    {
        _logger.LogInformation("=== STUDENT CREATE DEBUG START ===");
        _logger.LogInformation("BatchId: {BatchId}, TimingId: {TimingId}", model.BatchId, model.TimingId);
        
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
                        _logger.LogWarning("{Key}: {Error}", key, error.ErrorMessage ?? error.Exception?.Message);
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
            // Handle photo upload
            string? photoPath = null;
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                photoPath = await SavePhotoAsync(model.PhotoFile);
            }

            var success = await _studentService.CreateStudentAsync(model, photoPath);
            if (success)
            {
                TempData["SuccessMessage"] = "Student created successfully!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Failed to create student. Registration number may already exist.");
            }
        }

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // GET: Students/Edit/5 - Accounts can edit all, Teacher can edit assigned students only
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts},{UserRoles.Teacher}")]
    public async Task<IActionResult> Edit(int id)
    {
        StudentViewModel? student;
        
        if (User.IsInRole(UserRoles.Teacher))
        {
            // Teachers can only edit students assigned to their batches
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            student = await _studentService.GetStudentByIdForTeacherAsync(id, teacherId!);
        }
        else
        {
            // SuperAdmin, Accounts can edit any student
            student = await _studentService.GetStudentByIdAsync(id);
        }
        
        if (student == null)
        {
            return NotFound();
        }
        
        return View(student);
    }

    // POST: Students/Edit/5 - Accounts can edit all, Teacher can edit assigned students only
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts},{UserRoles.Teacher}")]
    public async Task<IActionResult> Edit(int id, StudentViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Handle photo upload
            string? photoPath = null;
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                photoPath = await SavePhotoAsync(model.PhotoFile);
            }

            var success = await _studentService.UpdateStudentAsync(model, photoPath);
            if (success)
            {
                TempData["SuccessMessage"] = "Student updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Failed to update student. Registration number may already exist.");
            }
        }

        await PopulateSelectListsAsync(model);
        return View(model);
    }

    // GET: Students/Delete/5 - Only Accounts role can delete students
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student == null)
        {
            return NotFound();
        }
        return View(student);
    }

    // POST: Students/Delete/5 - Only Accounts role can delete students
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var success = await _studentService.DeleteStudentAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Student deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete student.";
        }
        return RedirectToAction(nameof(Index));
    }

    // AJAX: Generate Registration Number
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRegistrationNumber(int tradeId, int sessionId)
    {
        try
        {
            _logger.LogInformation("Generating registration number for TradeId: {TradeId}, SessionId: {SessionId}", tradeId, sessionId);
            
            if (tradeId <= 0 || sessionId <= 0)
            {
                return Json(new { success = false, message = "Invalid trade or session selected" });
            }
            
            var registrationNumber = await _studentService.GenerateRegistrationNumberAsync(tradeId, sessionId);
            
            if (string.IsNullOrEmpty(registrationNumber))
            {
                return Json(new { success = false, message = "Failed to generate registration number" });
            }
            
            _logger.LogInformation("Generated registration number: {RegistrationNumber}", registrationNumber);
            return Json(new { success = true, registrationNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating registration number");
            return Json(new { success = false, message = "An error occurred while generating registration number" });
        }
    }

    // AJAX: Check Registration Number Uniqueness
    [HttpPost]
    public async Task<IActionResult> CheckRegistrationNumber(string registrationNumber, int? excludeId = null)
    {
        var isUnique = await _studentService.IsRegistrationNumberUniqueAsync(registrationNumber, excludeId);
        return Json(new { isUnique });
    }

    // AJAX: Get Batches by Trade and Session
    [HttpGet]
    public async Task<IActionResult> GetBatches(int tradeId, int sessionId)
    {
        try
        {
            _logger.LogInformation("Getting batches for TradeId: {TradeId}, SessionId: {SessionId}", tradeId, sessionId);
            
            var batches = await _dropdownService.GetBatchesAsync(tradeId, sessionId, null);
            
            // Convert SelectList to simple array for JavaScript
            var batchList = batches.Select(b => new { 
                value = b.Value, 
                text = b.Text 
            }).ToList();
            
            _logger.LogInformation("Found {Count} batches", batchList.Count);
            return Json(batchList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batches");
            return Json(new List<object>());
        }
    }

    // AJAX: Get Timings for a Batch
    [HttpGet]
    public async Task<IActionResult> GetBatchTimings(int batchId)
    {
        try
        {
            _logger.LogInformation("Getting timings for BatchId: {BatchId}", batchId);
            
            // Get batch timings from BatchTiming table
            var batchTimings = await _studentService.GetBatchTimingsAsync(batchId);
            
            if (batchTimings != null && batchTimings.Any())
            {
                var timingList = batchTimings.Select(bt => new {
                    value = bt.TimingId.ToString(),
                    text = bt.TimingDescription,
                    maxStudents = bt.MaxStudents,
                    currentStudents = bt.CurrentStudents,
                    availableSeats = bt.AvailableSeats
                }).ToList();
                
                _logger.LogInformation("Found {Count} timings for batch", timingList.Count);
                return Json(timingList);
            }
            
            // Fallback: if no batch timings, return the single timing from Batch table
            var batch = await _studentService.GetBatchByIdAsync(batchId);
            if (batch?.TimingId != null)
            {
                var timing = await _studentService.GetTimingByIdAsync(batch.TimingId.Value);
                if (timing != null)
                {
                    var fallbackList = new[] {
                        new {
                            value = timing.Id.ToString(),
                            text = timing.Name,
                            maxStudents = batch.MaxStudents,
                            currentStudents = batch.CurrentEnrollment,
                            availableSeats = batch.MaxStudents - batch.CurrentEnrollment
                        }
                    };
                    return Json(fallbackList);
                }
            }
            
            _logger.LogWarning("No timings found for batch {BatchId}", batchId);
            return Json(new List<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch timings");
            return Json(new List<object>());
        }
    }

    // AJAX: Assign to Batch
    [HttpPost]
    public async Task<IActionResult> AssignToBatch(int studentId, int batchId)
    {
        var success = await _studentService.AssignToBatchAsync(studentId, batchId);
        if (success)
        {
            return Json(new { success = true, message = "Student assigned to batch successfully!" });
        }
        return Json(new { success = false, message = "Failed to assign student to batch. Batch may be full." });
    }

    // AJAX: Provision student login (default password and force change)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
    public async Task<IActionResult> ProvisionLogin(int studentId)
    {
        var success = await _studentService.ProvisionStudentLoginAsync(studentId);
        if (success)
return Json(new { success = true, message = "Student login provisioned. Default password: Sostti123+" });
        return Json(new { success = false, message = "Could not provision student login." });
    }

    // Bulk: provision all unlinked student logins
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
    public async Task<IActionResult> ProvisionAllLogins()
    {
        var (created, skipped, failed) = await _studentService.ProvisionAllUnlinkedStudentLoginsAsync();
TempData["SuccessMessage"] = $"Provisioned: {created}, Skipped: {skipped}, Failed: {failed}. Default password: Sostti123+";
        return RedirectToAction(nameof(Index));
    }

    // AJAX: Remove from Batch
    [HttpPost]
    public async Task<IActionResult> RemoveFromBatch(int studentId)
    {
        var success = await _studentService.RemoveFromBatchAsync(studentId);
        if (success)
        {
            return Json(new { success = true, message = "Student removed from batch successfully!" });
        }
        return Json(new { success = false, message = "Failed to remove student from batch." });
    }

    // GET: Students/Profile - Student can view their own profile
    [Authorize(Roles = UserRoles.Student)]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await _studentService.GetStudentProfileAsync(userId!);
        
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student profile not found. Please contact administration.";
            return RedirectToAction("Index", "Dashboard");
        }
        
        return View("Details", student); // Reuse Details view
    }

    // GET: Students/ByStatus - Admin and SuperAdmin can view students grouped by status
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
    public async Task<IActionResult> ByStatus()
    {
        var studentsByStatus = await _studentService.GetStudentsCountByStatusAsync();
        return View(studentsByStatus);
    }

    private async Task PopulateSelectListsAsync(StudentViewModel model)
    {
        model.Trades = await _dropdownService.GetTradesAsync(model.TradeId);
        model.Sessions = await _dropdownService.GetSessionsAsync(model.SessionId);
        // Load all active batches initially, filter by AJAX when trade/session selected
        model.Batches = await _dropdownService.GetBatchesAsync(null, null, model.BatchId);
        model.GenderOptions = _dropdownService.GetGenderOptions(model.Gender);
        model.StatusOptions = _dropdownService.GetStatusOptions(model.Status);
    }

    private async Task<string?> SavePhotoAsync(IFormFile photoFile)
    {
        try
        {
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "students");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(photoFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photoFile.CopyToAsync(stream);
            }

            return $"/uploads/students/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving photo");
            return null;
        }
    }
}