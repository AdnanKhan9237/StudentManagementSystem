using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

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

    // GET: FeeManagement/CertificationFees
    public async Task<IActionResult> CertificationFees(int page = 1, string searchTerm = "", string statusFilter = "")
    {
        const int pageSize = 20;
        
        var query = _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => s.FirstName.Contains(searchTerm) ||
                                   s.LastName!.Contains(searchTerm) ||
                                   s.RegistrationNumber.Contains(searchTerm));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "applied":
                    query = query.Where(s => s.IsCertificationFeeApplicable);
                    break;
                case "notapplied":
                    query = query.Where(s => !s.IsCertificationFeeApplicable);
                    break;
                case "paid":
                    query = query.Where(s => s.IsCertificationFeeApplicable && s.CertificationFeePaid >= s.CertificationFee);
                    break;
                case "pending":
                    query = query.Where(s => s.IsCertificationFeeApplicable && s.CertificationFeePaid < s.CertificationFee);
                    break;
            }
        }

        var totalRecords = await query.CountAsync();

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new CertificationFeeStudentViewModel
            {
                StudentId = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                StudentName = s.FirstName + " " + (s.LastName ?? ""),
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name,
                PreviousQualification = s.PreviousQualification ?? "N/A",
                AdmissionDate = s.AdmissionDate,
                IsCertificationFeeApplicable = s.IsCertificationFeeApplicable,
                CertificationFee = s.CertificationFee,
                CertificationFeePaid = s.CertificationFeePaid,
                CertificationFeeRemaining = s.CertificationFee - s.CertificationFeePaid,
                PaymentStatus = !s.IsCertificationFeeApplicable ? "Not Applied" :
                               s.CertificationFeePaid >= s.CertificationFee ? "Paid" : 
                               s.CertificationFeePaid > 0 ? "Partial" : "Unpaid",
                CertificationFeeAppliedDate = s.CertificationFeeAppliedDate,
                CertificationStatus = s.CertificationStatus,
                CertificationReadyDate = s.CertificationReadyDate,
                CertificationHandedDate = s.CertificationHandedDate
            })
            .ToListAsync();

        var model = new CertificationFeeListViewModel
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

    // GET: FeeManagement/ApplyCertificationFee/5
    public async Task<IActionResult> ApplyCertificationFee(int id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        // Determine qualification level and fee
        var qualificationLevel = DetermineQualificationLevel(student.PreviousQualification);
        var certificationFee = await GetCertificationFeeForQualification(qualificationLevel);

        var model = new ApplyCertificationFeeViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            RegistrationNumber = student.RegistrationNumber,
            PreviousQualification = student.PreviousQualification ?? "N/A",
            QualificationLevel = qualificationLevel,
            CertificationFeeAmount = certificationFee,
            IsAlreadyApplied = student.IsCertificationFeeApplicable,
            AppliedDate = student.CertificationFeeAppliedDate
        };

        return View(model);
    }

    // POST: FeeManagement/ApplyCertificationFee/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCertificationFee(int id, decimal certificationFeeAmount)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        if (student.IsCertificationFeeApplicable)
        {
            TempData["ErrorMessage"] = "Certification fee has already been applied to this student.";
            return RedirectToAction(nameof(CertificationFees));
        }

        // Validate certification fee amount
        if (certificationFeeAmount <= 0)
        {
            TempData["ErrorMessage"] = "Certification fee amount must be greater than zero.";
            return RedirectToAction(nameof(ApplyCertificationFee), new { id });
        }

        if (certificationFeeAmount > 100000)
        {
            TempData["ErrorMessage"] = "Certification fee amount seems too high. Please verify the amount.";
            return RedirectToAction(nameof(ApplyCertificationFee), new { id });
        }

        // Apply certification fee
        student.CertificationFee = certificationFeeAmount;
        student.IsCertificationFeeApplicable = true;
        student.CertificationFeeAppliedDate = DateTime.UtcNow;
        student.ModifiedDate = DateTime.UtcNow;
        student.ModifiedBy = User.Identity?.Name ?? "System";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Certification fee of ₨{certificationFeeAmount:N0} applied successfully to {student.FirstName} {student.LastName}!";
        return RedirectToAction(nameof(CertificationFees));
    }

    // GET: FeeManagement/CollectCertificationFee/5
    public async Task<IActionResult> CollectCertificationFee(int id)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        if (!student.IsCertificationFeeApplicable)
        {
            TempData["ErrorMessage"] = "Certification fee has not been applied to this student yet.";
            return RedirectToAction(nameof(CertificationFees));
        }

        var qualificationLevel = DetermineQualificationLevel(student.PreviousQualification);

        var model = new CertificationFeeCollectionViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            RegistrationNumber = student.RegistrationNumber,
            TradeName = student.Trade.NameEnglish,
            SessionName = student.Session.Name,
            PreviousQualification = student.PreviousQualification ?? "N/A",
            QualificationLevel = qualificationLevel,
            CertificationFee = student.CertificationFee,
            CertificationFeePaid = student.CertificationFeePaid,
            RemainingAmount = student.CertificationFee - student.CertificationFeePaid,
            PaymentDate = DateTime.Now,
            PaymentMethod = "Cash"
        };

        return View(model);
    }

    // POST: FeeManagement/CollectCertificationFee
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectCertificationFee(CertificationFeeCollectionViewModel model)
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

            var remaining = student.CertificationFee - student.CertificationFeePaid;
            if (model.Amount > remaining)
            {
                ModelState.AddModelError("Amount", "Payment amount cannot exceed remaining certification fee.");
                return View(model);
            }

            // Generate receipt number
            var receiptNumber = await GenerateReceiptNumberAsync();

            // Create fee payment record with Certification fee type
            var payment = new FeePayment
            {
                StudentId = model.StudentId,
                Amount = model.Amount,
                PaymentDate = model.PaymentDate,
                PaymentMethod = model.PaymentMethod,
                FeeType = "Certification",
                ReceiptNumber = receiptNumber,
                Remarks = model.Remarks,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.FeePayments.Add(payment);

            // Update student's certification fee paid amount
            student.CertificationFeePaid += model.Amount;
            
            // Update certification status to FeePaid if fully paid
            if (student.CertificationFeePaid >= student.CertificationFee)
            {
                student.CertificationStatus = "FeePaid";
            }
            else if (string.IsNullOrEmpty(student.CertificationStatus))
            {
                student.CertificationStatus = "FeeNotPaid";
            }
            
            student.ModifiedDate = DateTime.UtcNow;
            student.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Certification fee payment of ₨{model.Amount:N0} collected successfully! Receipt: {receiptNumber}";
            return RedirectToAction(nameof(CertificationFees));
        }

        return View(model);
    }

    // GET: FeeManagement/UpdateCertificationStatus/5
    public async Task<IActionResult> UpdateCertificationStatus(int id)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null)
        {
            return NotFound();
        }

        var feeStatus = student.CertificationFeePaid >= student.CertificationFee ? "Paid" :
                       student.CertificationFeePaid > 0 ? "Partial" : "Not Paid";

        var model = new UpdateCertificationStatusViewModel
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            RegistrationNumber = student.RegistrationNumber,
            TradeName = student.Trade.NameEnglish,
            PreviousQualification = student.PreviousQualification ?? "N/A",
            FeePaymentStatus = feeStatus,
            CertificationStatus = student.CertificationStatus ?? "FeeNotPaid",
            CertificationReadyDate = student.CertificationReadyDate,
            CertificationHandedDate = student.CertificationHandedDate,
            CertificationRemarks = student.CertificationRemarks,
            CurrentStatus = student.CertificationStatus,
            CurrentReadyDate = student.CertificationReadyDate,
            CurrentHandedDate = student.CertificationHandedDate
        };

        return View(model);
    }

    // POST: FeeManagement/UpdateCertificationStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCertificationStatus(UpdateCertificationStatusViewModel model)
    {
        if (ModelState.IsValid)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == model.StudentId && !s.IsDeleted);

            if (student == null)
            {
                return NotFound();
            }

            // Validate status transitions
            if (model.CertificationStatus == "FeePaid" && student.CertificationFeePaid < student.CertificationFee)
            {
                TempData["ErrorMessage"] = "Cannot mark as FeePaid - certification fee is not fully paid yet.";
                return RedirectToAction(nameof(UpdateCertificationStatus), new { id = model.StudentId });
            }

            if (model.CertificationStatus == "Ready" && !model.CertificationReadyDate.HasValue)
            {
                ModelState.AddModelError("CertificationReadyDate", "Ready date is required when marking as Ready.");
                return View(model);
            }

            if (model.CertificationStatus == "Handed" && !model.CertificationHandedDate.HasValue)
            {
                ModelState.AddModelError("CertificationHandedDate", "Handed date is required when marking as Handed.");
                return View(model);
            }

            // Update certification status
            student.CertificationStatus = model.CertificationStatus;
            student.CertificationReadyDate = model.CertificationReadyDate;
            student.CertificationHandedDate = model.CertificationHandedDate;
            student.CertificationRemarks = model.CertificationRemarks;
            student.ModifiedDate = DateTime.UtcNow;
            student.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Certification status updated successfully for {student.FirstName} {student.LastName}!";
            return RedirectToAction(nameof(CertificationFees));
        }

        return View(model);
    }

    private string DetermineQualificationLevel(string? previousQualification)
    {
        if (string.IsNullOrWhiteSpace(previousQualification))
            return "UnderMatric";

        var qual = previousQualification.ToLower();
        
        if (qual.Contains("bachelor") || qual.Contains("master") || qual.Contains("phd"))
            return "AboveIntermediate";
        
        if (qual.Contains("intermediate") || qual.Contains("inter"))
            return "Intermediate";
        
        if (qual.Contains("matric") || qual.Contains("ssc"))
            return "Matric";
        
        return "UnderMatric";
    }

    private async Task<decimal> GetCertificationFeeForQualification(string qualificationLevel)
    {
        var config = await _context.CertificationFeeConfigs
            .Where(c => c.QualificationLevel == qualificationLevel && c.IsActive && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedDate)
            .FirstOrDefaultAsync();

        if (config != null)
            return config.FeeAmount;

        // Default fees if not configured
        return qualificationLevel switch
        {
            "UnderMatric" => 1500m,
            "Matric" => 2000m,
            "Intermediate" => 2000m,
            "AboveIntermediate" => 3000m,
            _ => 1500m
        };
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
