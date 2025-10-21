using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers;

[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Accounts}")]
public class FeeManagementController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeeManagementController> _logger;

    public FeeManagementController(ApplicationDbContext context, ILogger<FeeManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: FeeManagement
    public async Task<IActionResult> Index(int page = 1, string searchTerm = "", string statusFilter = "")
    {
        const int pageSize = 20;
        
        var query = _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => s.FirstName.Contains(searchTerm) ||
                                   s.LastName.Contains(searchTerm) ||
                                   s.RegistrationNumber.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "paid":
                    query = query.Where(s => s.PaidAmount >= s.TotalFee);
                    break;
                case "partial":
                    query = query.Where(s => s.PaidAmount > 0 && s.PaidAmount < s.TotalFee);
                    break;
                case "unpaid":
                    query = query.Where(s => s.PaidAmount <= 0);
                    break;
            }
        }

        var totalRecords = await query.CountAsync();

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new FeeStatusViewModel
            {
                StudentId = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                StudentName = s.FirstName + " " + s.LastName,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name,
                TotalFee = s.TotalFee,
                PaidAmount = s.PaidAmount,
                RemainingAmount = s.TotalFee - s.PaidAmount,
                PaymentStatus = s.PaidAmount >= s.TotalFee ? "Paid" : 
                               s.PaidAmount > 0 ? "Partial" : "Unpaid"
            })
            .ToListAsync();

        var model = new FeeManagementListViewModel
        {
            Students = students,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };

        return View(model);
    }

    // GET: FeeManagement/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.FeePayments)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        var model = new StudentFeeDetailsViewModel
        {
            StudentId = student.Id,
            RegistrationNumber = student.RegistrationNumber,
            StudentName = $"{student.FirstName} {student.LastName}",
            TradeName = student.Trade.NameEnglish,
            SessionName = student.Session.Name,
            TotalFee = student.TotalFee,
            PaidAmount = student.PaidAmount,
            RemainingAmount = student.TotalFee - student.PaidAmount,
            PaymentStatus = student.PaidAmount >= student.TotalFee ? "Paid" : 
                           student.PaidAmount > 0 ? "Partial" : "Unpaid",
            PaymentHistory = student.FeePayments.Select(p => new PaymentHistoryViewModel
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                ReceiptNumber = p.ReceiptNumber,
                Remarks = p.Remarks,
                CreatedBy = p.CreatedBy
            }).OrderByDescending(p => p.PaymentDate).ToList()
        };

        return View(model);
    }

    // GET: FeeManagement/CollectFee/5
    public async Task<IActionResult> CollectFee(int id)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        var model = new FeeCollectionViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            RegistrationNumber = student.RegistrationNumber,
            TradeName = student.Trade.NameEnglish,
            SessionName = student.Session.Name,
            TotalFee = student.TotalFee,
            PaidAmount = student.PaidAmount,
            RemainingAmount = student.TotalFee - student.PaidAmount,
            PaymentDate = DateTime.Now,
            PaymentMethod = "Cash"
        };

        return View(model);
    }

    // POST: FeeManagement/CollectFee
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectFee(FeeCollectionViewModel model)
    {
        if (ModelState.IsValid)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == model.StudentId);
            if (student == null)
            {
                return NotFound();
            }

            // Validate amount
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Payment amount must be greater than zero.");
                return View(model);
            }

            if (model.Amount > (student.TotalFee - student.PaidAmount))
            {
                ModelState.AddModelError("Amount", "Payment amount cannot exceed remaining fee.");
                return View(model);
            }

            // Generate receipt number
            var receiptNumber = await GenerateReceiptNumberAsync();

            // Create fee payment record
            var payment = new FeePayment
            {
                StudentId = model.StudentId,
                Amount = model.Amount,
                PaymentDate = model.PaymentDate,
                PaymentMethod = model.PaymentMethod,
                ReceiptNumber = receiptNumber,
                Remarks = model.Remarks,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.FeePayments.Add(payment);

            // Update student's paid amount
            student.PaidAmount += model.Amount;
            student.ModifiedDate = DateTime.UtcNow;
            student.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fee payment of ₨{model.Amount:N0} collected successfully! Receipt: {receiptNumber}";
            return RedirectToAction(nameof(Details), new { id = model.StudentId });
        }

        return View(model);
    }

    // GET: FeeManagement/Reports
    public async Task<IActionResult> Reports()
    {
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted);
        var totalFeeAmount = await _context.Students.Where(s => !s.IsDeleted).SumAsync(s => s.TotalFee);
        var totalCollected = await _context.Students.Where(s => !s.IsDeleted).SumAsync(s => s.PaidAmount);
        var totalPending = totalFeeAmount - totalCollected;

        var paidStudents = await _context.Students.CountAsync(s => !s.IsDeleted && s.PaidAmount >= s.TotalFee);
        var partialStudents = await _context.Students.CountAsync(s => !s.IsDeleted && s.PaidAmount > 0 && s.PaidAmount < s.TotalFee);
        var unpaidStudents = await _context.Students.CountAsync(s => !s.IsDeleted && s.PaidAmount <= 0);

        var tradeWiseData = await _context.Students
            .Include(s => s.Trade)
            .Where(s => !s.IsDeleted)
            .GroupBy(s => s.Trade.NameEnglish)
            .Select(g => new TradeWiseFeeViewModel
            {
                TradeName = g.Key,
                StudentCount = g.Count(),
                TotalFee = g.Sum(s => s.TotalFee),
                CollectedAmount = g.Sum(s => s.PaidAmount),
                PendingAmount = g.Sum(s => s.TotalFee - s.PaidAmount)
            })
            .ToListAsync();

        var recentPayments = await _context.FeePayments
            .Include(p => p.Student)
            .OrderByDescending(p => p.PaymentDate)
            .Take(10)
            .Select(p => new PaymentHistoryViewModel
            {
                StudentName = p.Student.FirstName + " " + p.Student.LastName,
                RegistrationNumber = p.Student.RegistrationNumber,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                ReceiptNumber = p.ReceiptNumber,
                CreatedBy = p.CreatedBy
            })
            .ToListAsync();

        var model = new FeeReportsViewModel
        {
            TotalStudents = totalStudents,
            TotalFeeAmount = totalFeeAmount,
            TotalCollected = totalCollected,
            TotalPending = totalPending,
            CollectionRate = totalFeeAmount > 0 ? (double)(totalCollected / totalFeeAmount) * 100 : 0,
            PaidStudents = paidStudents,
            PartialStudents = partialStudents,
            UnpaidStudents = unpaidStudents,
            TradeWiseData = tradeWiseData,
            RecentPayments = recentPayments
        };

        return View(model);
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var currentYear = DateTime.Now.Year;
        var yearPrefix = currentYear.ToString().Substring(2);
        
        var lastReceipt = await _context.FeePayments
            .Where(p => p.ReceiptNumber.StartsWith($"RCP{yearPrefix}"))
            .OrderByDescending(p => p.ReceiptNumber)
            .Select(p => p.ReceiptNumber)
            .FirstOrDefaultAsync();

        int sequenceNumber = 1;
        if (lastReceipt != null)
        {
            var lastSequence = lastReceipt.Substring(5); // Remove "RCPxx" prefix
            if (int.TryParse(lastSequence, out int lastNumber))
            {
                sequenceNumber = lastNumber + 1;
            }
        }

        return $"RCP{yearPrefix}{sequenceNumber:D4}";
    }
}