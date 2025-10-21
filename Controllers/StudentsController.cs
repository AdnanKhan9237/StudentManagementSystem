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
    public async Task<IActionResult> GenerateRegistrationNumber(int tradeId, int sessionId)
    {
        var registrationNumber = await _studentService.GenerateRegistrationNumberAsync(tradeId, sessionId);
        return Json(new { registrationNumber });
    }

    // AJAX: Check Registration Number Uniqueness
    [HttpPost]
    public async Task<IActionResult> CheckRegistrationNumber(string registrationNumber, int? excludeId = null)
    {
        var isUnique = await _studentService.IsRegistrationNumberUniqueAsync(registrationNumber, excludeId);
        return Json(new { isUnique });
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
        model.Batches = await _dropdownService.GetBatchesAsync(model.TradeId, model.SessionId, model.BatchId);
        model.GenderOptions = _dropdownService.GetGenderOptions(model.Gender);
        model.StatusOptions = _dropdownService.GetStatusOptions(model.Status);
    }

    private async Task<string> SavePhotoAsync(IFormFile photoFile)
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