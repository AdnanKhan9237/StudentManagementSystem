using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;
namespace StudentManagementSystem.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Get current user roles
            var userRoles = await GetCurrentUserRolesAsync();
            
            var viewModel = new DashboardViewModel();
        
        // Set dashboard type based on role
        if (userRoles.Contains(UserRoles.SuperAdmin))
        {
            viewModel.DashboardType = "SuperAdmin";
            viewModel.TotalUsers = await _userManager.Users.CountAsync();
        }
        else if (userRoles.Contains(UserRoles.Admin))
        {
            viewModel.DashboardType = "Admin";
            viewModel.TotalUsers = await _userManager.Users.CountAsync();
        }
        else if (userRoles.Contains(UserRoles.Teacher))
        {
            viewModel.DashboardType = "Teacher";
        }
        else if (userRoles.Contains(UserRoles.Accounts))
        {
            viewModel.DashboardType = "Accounts";
        }
        else if (userRoles.Contains(UserRoles.Student))
        {
            return await StudentDashboard();
        }
        else
        {
            viewModel.DashboardType = "Default";
        }

        // Basic Statistics (visible to most roles)
        viewModel.TotalStudents = await _context.Students.CountAsync(s => !s.IsDeleted);
        viewModel.ActiveStudents = await _context.Students.CountAsync(s => s.Status == "Active" && !s.IsDeleted);
        viewModel.TotalTrades = await _context.Trades.CountAsync(t => t.IsActive && !t.IsDeleted);
        viewModel.TotalBatches = await _context.Batches.CountAsync(b => b.Status == "Active" && !b.IsDeleted);
        viewModel.TotalTeachers = await _context.Teachers.CountAsync(t => !t.IsDeleted);
        viewModel.ActiveTeachers = await _context.Teachers.CountAsync(t => t.Status == "Active" && !t.IsDeleted);

        // Fee Statistics (for SuperAdmin, Admin, Accounts)
        if (userRoles.Contains(UserRoles.SuperAdmin) || userRoles.Contains(UserRoles.Admin) || userRoles.Contains(UserRoles.Accounts))
        {
            var studentsWithFees = await _context.Students
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            viewModel.TotalFeeAmount = studentsWithFees.Sum(s => s.TotalFee);
            viewModel.CollectedFeeAmount = studentsWithFees.Sum(s => s.PaidAmount);
            viewModel.PendingFeeAmount = studentsWithFees.Sum(s => s.RemainingAmount);
            viewModel.StudentsPendingFees = studentsWithFees.Count(s => s.PaidAmount < s.TotalFee);
        }

        // Recent Students (for SuperAdmin, Admin, Teacher)
        if (userRoles.Contains(UserRoles.SuperAdmin) || userRoles.Contains(UserRoles.Admin) || userRoles.Contains(UserRoles.Teacher))
        {
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            viewModel.RecentStudents = await _context.Students
                .Where(s => s.CreatedDate >= lastWeek && !s.IsDeleted)
                .Include(s => s.Trade)
                .Include(s => s.Session)
                .OrderByDescending(s => s.CreatedDate)
                .Take(5)
                .Select(s => new RecentStudentDto
                {
                    Id = s.Id,
                    FullName = s.FirstName + " " + s.LastName,
                    RegistrationNumber = s.RegistrationNumber,
                    TradeName = s.Trade.NameEnglish,
                    AdmissionDate = s.AdmissionDate,
                    PhotoPath = s.PhotoPath
                })
                .ToListAsync();
        }

        // Chart data (for SuperAdmin and Admin)
        if (userRoles.Contains(UserRoles.SuperAdmin) || userRoles.Contains(UserRoles.Admin))
        {
            // Students by Status - Optimized query
            var statusCounts = await _context.Students
                .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.Status))
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            viewModel.StudentsByStatus = statusCounts
                .Select(s => new StatusCountDto
                {
                    Status = s.Status,
                    Count = s.Count
                })
                .OrderByDescending(x => x.Count)
                .Take(10) // Limit to top 10 statuses to prevent UI overflow
                .ToList();

            // Students by Trade (Top 5)
            viewModel.StudentsByTrade = await _context.Students
                .Where(s => !s.IsDeleted)
                .Include(s => s.Trade)
                .GroupBy(s => s.Trade.NameEnglish)
                .Select(g => new TradeCountDto
                {
                    TradeName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // Monthly Admissions (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            viewModel.MonthlyAdmissions = await _context.Students
                .Where(s => s.AdmissionDate >= sixMonthsAgo && !s.IsDeleted)
                .GroupBy(s => new { s.AdmissionDate.Year, s.AdmissionDate.Month })
                .Select(g => new MonthlyAdmissionDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Low Capacity Trades (less than 30% filled) - Optimized query
            var tradeCapacities = await _context.Trades
                .Where(t => t.IsActive && !t.IsDeleted && t.MaxStudents > 0)
                .Select(t => new
                {
                    Id = t.Id,
                    TradeName = t.NameEnglish,
                    MaxStudents = t.MaxStudents,
                    CurrentStudents = _context.Students.Count(s => s.TradeId == t.Id && !s.IsDeleted)
                })
                .ToListAsync();

            viewModel.LowCapacityTrades = tradeCapacities
                .Select(t => new TradeCapacityDto
                {
                    Id = t.Id,
                    TradeName = t.TradeName,
                    MaxStudents = t.MaxStudents,
                    CurrentStudents = t.CurrentStudents,
                    FillPercentage = t.MaxStudents > 0 ? (double)t.CurrentStudents / t.MaxStudents * 100 : 0
                })
                .Where(t => t.FillPercentage < 30)
                .OrderBy(t => t.FillPercentage)
                .Take(5)
                .ToList();
        }

        return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {UserId}", User.Identity?.Name);
            ViewBag.ErrorMessage = "There was an error loading the dashboard. Please try again later.";
            return View(new DashboardViewModel { DashboardType = "Error" });
        }
    }

    private async Task<IActionResult> StudentDashboard()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.UserId == currentUser.Id && !s.IsDeleted);

        if (student == null)
        {
            ViewBag.Message = "No student profile found for this user.";
            return View("StudentDashboard", new DashboardViewModel { DashboardType = "Student" });
        }

        var viewModel = new DashboardViewModel
        {
            DashboardType = "Student",
            CurrentStudent = new StudentProfileDto
            {
                Id = student.Id,
                RegistrationNumber = student.RegistrationNumber,
                FullName = $"{student.FirstName} {student.LastName}",
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                PhotoPath = student.PhotoPath,
                Status = student.Status,
                TradeName = student.Trade?.NameEnglish ?? "Not Assigned",
                SessionName = student.Session?.Name ?? "Not Assigned",
                BatchName = student.Batch?.Name ?? "Not Assigned",
                TotalFee = student.TotalFee,
                PaidAmount = student.PaidAmount,
                RemainingAmount = student.RemainingAmount,
                AdmissionDate = student.AdmissionDate
            }
        };

        return View("StudentDashboard", viewModel);
    }
    private async Task<List<string>> GetCurrentUserRolesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return new List<string>();
        return (await _userManager.GetRolesAsync(user)).ToList();
    }
}

