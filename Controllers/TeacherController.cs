using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;
using System.Text;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TeacherController> _logger;
    private readonly IWebHostEnvironment _environment;

    public TeacherController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ILogger<TeacherController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _environment = environment;
    }

    // GET: Teacher
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", string status = "")
    {
        const int pageSize = 15;
        
        var query = _context.Teachers
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => t.TeacherCode.Contains(searchTerm) ||
                                   t.FirstName.Contains(searchTerm) ||
                                   t.LastName.Contains(searchTerm) ||
                                   t.Username.Contains(searchTerm) ||
                                   (t.CNIC != null && t.CNIC.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var totalRecords = await query.CountAsync();

        var teachers = await query
            .OrderBy(t => t.TeacherCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeacherViewModel
            {
                Id = t.Id,
                TeacherCode = t.TeacherCode,
                Username = t.Username,
                FirstName = t.FirstName,
                LastName = t.LastName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                Status = t.Status,
                HireDate = t.HireDate,
                Qualification = t.Qualification,
                Specialization = t.Specialization,
                CreatedDate = t.CreatedDate,
                CreatedBy = t.CreatedBy,
                PrimaryBatchCount = t.PrimaryBatches.Count(b => !b.IsDeleted),
                SecondaryBatchCount = t.SecondaryBatches.Count(b => !b.IsDeleted)
            })
            .ToListAsync();

        var model = new TeacherListViewModel
        {
            Teachers = teachers,
            SearchTerm = searchTerm,
            Status = status,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            StatusOptions = new SelectList(new[]
            {
                new { Value = "", Text = "All Status" },
                new { Value = "Active", Text = "Active" },
                new { Value = "Inactive", Text = "Inactive" },
                new { Value = "Terminated", Text = "Terminated" }
            }, "Value", "Text", status)
        };

        return View(model);
    }

    // GET: Teacher/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.PrimaryBatches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Trade)
            .Include(t => t.PrimaryBatches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Session)
            .Include(t => t.SecondaryBatches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Trade)
            .Include(t => t.SecondaryBatches.Where(b => !b.IsDeleted))
            .ThenInclude(b => b.Session)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (teacher == null)
        {
            return NotFound();
        }

        var model = new TeacherViewModel
        {
            Id = teacher.Id,
            TeacherCode = teacher.TeacherCode,
            Username = teacher.Username,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            FatherName = teacher.FatherName,
            CNIC = teacher.CNIC,
            DateOfBirth = teacher.DateOfBirth,
            Gender = teacher.Gender,
            PhoneNumber = teacher.PhoneNumber,
            Email = teacher.Email,
            Address = teacher.Address,
            City = teacher.City,
            Province = teacher.Province,
            Country = teacher.Country,
            PostalCode = teacher.PostalCode,
            EmergencyContactName = teacher.EmergencyContactName,
            EmergencyContactPhone = teacher.EmergencyContactPhone,
            PhotoPath = teacher.PhotoPath,
            HireDate = teacher.HireDate,
            Status = teacher.Status,
            Qualification = teacher.Qualification,
            Specialization = teacher.Specialization,
            Salary = teacher.Salary,
            Remarks = teacher.Remarks,
            CreatedDate = teacher.CreatedDate,
            CreatedBy = teacher.CreatedBy,
            ModifiedDate = teacher.ModifiedDate,
            ModifiedBy = teacher.ModifiedBy,
            PrimaryBatchCount = teacher.PrimaryBatches.Count,
            SecondaryBatchCount = teacher.SecondaryBatches.Count,
            AssignedBatches = teacher.PrimaryBatches.Select(b => $"{b.BatchCode} (Primary)").Concat(
                             teacher.SecondaryBatches.Select(b => $"{b.BatchCode} (Secondary)")).ToList()
        };

        return View(model);
    }

    // GET: Teacher/Create
    public IActionResult Create()
    {
        var model = new TeacherViewModel
        {
            HireDate = DateTime.Today,
            Status = "Active",
            StatusOptions = GetStatusOptions()
        };

        return View(model);
    }

    // POST: Teacher/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if teacher code is unique
            if (await _context.Teachers.AnyAsync(t => t.TeacherCode == model.TeacherCode && !t.IsDeleted))
            {
                ModelState.AddModelError("TeacherCode", "Teacher code already exists.");
                model.StatusOptions = GetStatusOptions();
                return View(model);
            }

            // Check if username is unique
            if (await _context.Teachers.AnyAsync(t => t.Username == model.Username && !t.IsDeleted))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                model.StatusOptions = GetStatusOptions();
                return View(model);
            }

            // Generate password if not provided
            if (string.IsNullOrEmpty(model.Password))
            {
                model.Password = GenerateRandomPassword();
            }

            try
            {
                // Create ApplicationUser first
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email ?? $"{model.Username}@school.edu",
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    model.StatusOptions = GetStatusOptions();
                    return View(model);
                }

                // Add to Teacher role
                await _userManager.AddToRoleAsync(user, UserRoles.Teacher);

                // Handle photo upload
                string? photoPath = null;
                if (model.Photo != null)
                {
                    photoPath = await SavePhotoAsync(model.Photo);
                }

                // Create Teacher record
                var teacher = new Teacher
                {
                    TeacherCode = model.TeacherCode,
                    Username = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    FatherName = model.FatherName,
                    CNIC = model.CNIC,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Address = model.Address,
                    City = model.City,
                    Province = model.Province,
                    Country = model.Country,
                    PostalCode = model.PostalCode,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    PhotoPath = photoPath,
                    HireDate = model.HireDate,
                    Status = model.Status,
                    Qualification = model.Qualification,
                    Specialization = model.Specialization,
                    Salary = model.Salary,
                    Remarks = model.Remarks,
                    UserId = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Teacher created successfully! Username: {model.Username}, Password: {model.Password}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher {TeacherCode}", model.TeacherCode);
                ModelState.AddModelError("", "An error occurred while creating the teacher. Please try again.");
            }
        }

        model.StatusOptions = GetStatusOptions();
        return View(model);
    }

    // GET: Teacher/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (teacher == null)
        {
            return NotFound();
        }

        var model = new TeacherViewModel
        {
            Id = teacher.Id,
            TeacherCode = teacher.TeacherCode,
            Username = teacher.Username,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            FatherName = teacher.FatherName,
            CNIC = teacher.CNIC,
            DateOfBirth = teacher.DateOfBirth,
            Gender = teacher.Gender,
            PhoneNumber = teacher.PhoneNumber,
            Email = teacher.Email,
            Address = teacher.Address,
            City = teacher.City,
            Province = teacher.Province,
            Country = teacher.Country,
            PostalCode = teacher.PostalCode,
            EmergencyContactName = teacher.EmergencyContactName,
            EmergencyContactPhone = teacher.EmergencyContactPhone,
            PhotoPath = teacher.PhotoPath,
            HireDate = teacher.HireDate,
            Status = teacher.Status,
            Qualification = teacher.Qualification,
            Specialization = teacher.Specialization,
            Salary = teacher.Salary,
            Remarks = teacher.Remarks,
            StatusOptions = GetStatusOptions(teacher.Status)
        };

        return View(model);
    }

    // POST: Teacher/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check if teacher code is unique (excluding current teacher)
            if (await _context.Teachers.AnyAsync(t => t.TeacherCode == model.TeacherCode && t.Id != id && !t.IsDeleted))
            {
                ModelState.AddModelError("TeacherCode", "Teacher code already exists.");
                model.StatusOptions = GetStatusOptions();
                return View(model);
            }

            // Check if username is unique (excluding current teacher)
            if (await _context.Teachers.AnyAsync(t => t.Username == model.Username && t.Id != id && !t.IsDeleted))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                model.StatusOptions = GetStatusOptions();
                return View(model);
            }

            try
            {
                var teacher = await _context.Teachers.FindAsync(id);
                if (teacher == null || teacher.IsDeleted)
                {
                    return NotFound();
                }

                // Handle photo upload
                if (model.Photo != null)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(teacher.PhotoPath))
                    {
                        DeletePhoto(teacher.PhotoPath);
                    }
                    teacher.PhotoPath = await SavePhotoAsync(model.Photo);
                }

                // Update teacher fields
                teacher.TeacherCode = model.TeacherCode;
                teacher.Username = model.Username;
                teacher.FirstName = model.FirstName;
                teacher.LastName = model.LastName;
                teacher.FatherName = model.FatherName;
                teacher.CNIC = model.CNIC;
                teacher.DateOfBirth = model.DateOfBirth;
                teacher.Gender = model.Gender;
                teacher.PhoneNumber = model.PhoneNumber;
                teacher.Email = model.Email;
                teacher.Address = model.Address;
                teacher.City = model.City;
                teacher.Province = model.Province;
                teacher.Country = model.Country;
                teacher.PostalCode = model.PostalCode;
                teacher.EmergencyContactName = model.EmergencyContactName;
                teacher.EmergencyContactPhone = model.EmergencyContactPhone;
                teacher.HireDate = model.HireDate;
                teacher.Status = model.Status;
                teacher.Qualification = model.Qualification;
                teacher.Specialization = model.Specialization;
                teacher.Salary = model.Salary;
                teacher.Remarks = model.Remarks;
                teacher.ModifiedDate = DateTime.UtcNow;
                teacher.ModifiedBy = User.Identity?.Name ?? "System";

                // Update ApplicationUser if exists
                if (!string.IsNullOrEmpty(teacher.UserId))
                {
                    var user = await _userManager.FindByIdAsync(teacher.UserId);
                    if (user != null)
                    {
                        user.UserName = model.Username;
                        user.Email = model.Email ?? $"{model.Username}@school.edu";
                        user.FirstName = model.FirstName;
                        user.LastName = model.LastName;
                        user.PhoneNumber = model.PhoneNumber;

                        await _userManager.UpdateAsync(user);

                        // Update password if provided
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                            await _userManager.ResetPasswordAsync(user, token, model.Password);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Teacher updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the teacher.");
            }
        }

        model.StatusOptions = GetStatusOptions();
        return View(model);
    }

    // GET: Teacher/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.PrimaryBatches.Where(b => !b.IsDeleted))
            .Include(t => t.SecondaryBatches.Where(b => !b.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (teacher == null)
        {
            return NotFound();
        }

        var model = new TeacherViewModel
        {
            Id = teacher.Id,
            TeacherCode = teacher.TeacherCode,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Status = teacher.Status,
            PrimaryBatchCount = teacher.PrimaryBatches.Count,
            SecondaryBatchCount = teacher.SecondaryBatches.Count
        };

        return View(model);
    }

    // POST: Teacher/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null || teacher.IsDeleted)
            {
                return NotFound();
            }

            // Check if teacher has active batch assignments
            var hasActiveBatches = await _context.Batches
                .AnyAsync(b => (b.PrimaryInstructorId == id || b.SecondaryInstructorId == id) && 
                              b.Status == "Active" && !b.IsDeleted);

            if (hasActiveBatches)
            {
                TempData["ErrorMessage"] = "Cannot delete teacher because they have active batch assignments.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Soft delete
            teacher.IsDeleted = true;
            teacher.ModifiedDate = DateTime.UtcNow;
            teacher.ModifiedBy = User.Identity?.Name ?? "System";

            // Deactivate associated user account
            if (!string.IsNullOrEmpty(teacher.UserId))
            {
                var user = await _userManager.FindByIdAsync(teacher.UserId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Teacher deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting teacher {Id}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the teacher.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    // Helper methods
    private SelectList GetStatusOptions(string? selectedValue = null)
    {
        return new SelectList(new[]
        {
            new { Value = "Active", Text = "Active" },
            new { Value = "Inactive", Text = "Inactive" },
            new { Value = "Terminated", Text = "Terminated" }
        }, "Value", "Text", selectedValue);
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        var password = new StringBuilder();

        // Ensure at least one uppercase, one lowercase, and one number
        password.Append(chars[random.Next(0, 25)]);  // Uppercase
        password.Append(chars[random.Next(25, 49)]); // Lowercase  
        password.Append(chars[random.Next(49, 58)]); // Number

        // Add 5 more random characters
        for (int i = 0; i < 5; i++)
        {
            password.Append(chars[random.Next(chars.Length)]);
        }

        // Shuffle the password
        var shuffled = password.ToString().ToCharArray();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return new string(shuffled);
    }

    private async Task<string> SavePhotoAsync(IFormFile photo)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "teachers");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}_{photo.FileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await photo.CopyToAsync(stream);

        return $"/uploads/teachers/{fileName}";
    }

    private void DeletePhoto(string photoPath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, photoPath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete photo {PhotoPath}", photoPath);
        }
    }
}