using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using StudentManagementSystem.Data;
using StudentManagementSystem.Interfaces;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.Constants;
using System.Globalization;
using System.Text;

namespace StudentManagementSystem.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public StudentService(ApplicationDbContext context, ILogger<StudentService> logger, 
        UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<StudentListViewModel> GetStudentsAsync(int pageNumber, int pageSize, string searchTerm = "",
        int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "")
    {
        var query = _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => s.FirstName.Contains(searchTerm) ||
                                   s.LastName.Contains(searchTerm) ||
                                   s.RegistrationNumber.Contains(searchTerm) ||
                                   (s.CNIC != null && s.CNIC.Contains(searchTerm)));
        }

        if (tradeFilter.HasValue)
        {
            query = query.Where(s => s.TradeId == tradeFilter.Value);
        }

        if (sessionFilter.HasValue)
        {
            query = query.Where(s => s.SessionId == sessionFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(s => s.Status == statusFilter);
        }

        var totalRecords = await query.CountAsync();

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FatherName = s.FatherName,
                CNIC = s.CNIC,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                Address = s.Address,
                AdmissionDate = s.AdmissionDate,
                Status = s.Status,
                TradeId = s.TradeId,
                SessionId = s.SessionId,
                BatchId = s.BatchId,
                TotalFee = s.TotalFee,
                PaidAmount = s.PaidAmount,
                PhotoPath = s.PhotoPath,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name,
                BatchName = s.Batch != null ? s.Batch.BatchName : "Not Assigned"
            })
            .ToListAsync();

        // Get filter options
        var trades = await _context.Trades.Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish }).ToListAsync();
        
        var sessions = await _context.Sessions.Where(s => s.IsActive && !s.IsDeleted)
            .Select(s => new { s.Id, s.Name }).ToListAsync();

        var statusOptions = new List<SelectListItem>
        {
            new() { Value = "", Text = "All Status" },
            new() { Value = "Active", Text = "Active" },
            new() { Value = "Graduated", Text = "Graduated" },
            new() { Value = "Dropped", Text = "Dropped" },
            new() { Value = "Suspended", Text = "Suspended" }
        };

        return new StudentListViewModel
        {
            Students = students,
            SearchTerm = searchTerm,
            TradeFilter = tradeFilter,
            SessionFilter = sessionFilter,
            StatusFilter = statusFilter,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            Trades = new SelectList(trades, "Id", "NameEnglish"),
            Sessions = new SelectList(sessions, "Id", "Name"),
            StatusOptions = new SelectList(statusOptions, "Value", "Text")
        };
    }

    public async Task<StudentListViewModel> GetStudentsForTeacherAsync(string teacherId, int pageNumber, int pageSize,
        string searchTerm = "", int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "")
    {
        // Get teacher by user ID
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == teacherId && !t.IsDeleted);

        if (teacher == null)
        {
            return new StudentListViewModel();
        }

        // Get students assigned to batches where this teacher is primary or secondary instructor
        var query = _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted && 
                       s.Batch != null &&
                       (s.Batch.PrimaryInstructorId == teacher.Id || s.Batch.SecondaryInstructorId == teacher.Id))
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => s.FirstName.Contains(searchTerm) ||
                                   s.LastName.Contains(searchTerm) ||
                                   s.RegistrationNumber.Contains(searchTerm));
        }

        if (tradeFilter.HasValue)
        {
            query = query.Where(s => s.TradeId == tradeFilter.Value);
        }

        if (sessionFilter.HasValue)
        {
            query = query.Where(s => s.SessionId == sessionFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(s => s.Status == statusFilter);
        }

        var totalRecords = await query.CountAsync();

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FatherName = s.FatherName,
                CNIC = s.CNIC,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                Address = s.Address,
                AdmissionDate = s.AdmissionDate,
                Status = s.Status,
                TradeId = s.TradeId,
                SessionId = s.SessionId,
                BatchId = s.BatchId,
                TotalFee = s.TotalFee,
                PaidAmount = s.PaidAmount,
                PhotoPath = s.PhotoPath,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name,
                BatchName = s.Batch != null ? s.Batch.BatchName : "Not Assigned"
            })
            .ToListAsync();

        // Get filter options (same as regular method)
        var trades = await _context.Trades.Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish }).ToListAsync();
        
        var sessions = await _context.Sessions.Where(s => s.IsActive && !s.IsDeleted)
            .Select(s => new { s.Id, s.Name }).ToListAsync();

        var statusOptions = new List<SelectListItem>
        {
            new() { Value = "", Text = "All Status" },
            new() { Value = "Active", Text = "Active" },
            new() { Value = "Graduated", Text = "Graduated" },
            new() { Value = "Dropped", Text = "Dropped" },
            new() { Value = "Suspended", Text = "Suspended" }
        };

        return new StudentListViewModel
        {
            Students = students,
            SearchTerm = searchTerm,
            TradeFilter = tradeFilter,
            SessionFilter = sessionFilter,
            StatusFilter = statusFilter,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            Trades = new SelectList(trades, "Id", "NameEnglish"),
            Sessions = new SelectList(sessions, "Id", "Name"),
            StatusOptions = new SelectList(statusOptions, "Value", "Text")
        };
    }

    public async Task<StudentViewModel?> GetStudentByIdAsync(int id)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (student == null) return null;

        var viewModel = new StudentViewModel
        {
            Id = student.Id,
            RegistrationNumber = student.RegistrationNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            FatherName = student.FatherName,
            CNIC = student.CNIC,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            BloodGroup = student.BloodGroup,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            Address = student.Address,
            City = student.City,
            Province = student.Province,
            PostalCode = student.PostalCode,
            EmergencyContactName = student.EmergencyContactName,
            EmergencyContactPhone = student.EmergencyContactPhone,
            PhotoPath = student.PhotoPath,
            AdmissionDate = student.AdmissionDate,
            Status = student.Status,
            TradeId = student.TradeId,
            SessionId = student.SessionId,
            BatchId = student.BatchId,
            TimingId = student.TimingId,
            PreviousQualification = student.PreviousQualification,
            PreviousInstitute = student.PreviousInstitute,
            PreviousMarks = student.PreviousMarks,
            TotalFee = student.TotalFee,
            PaidAmount = student.PaidAmount,
            Remarks = student.Remarks,
            TradeName = student.Trade.NameEnglish,
            SessionName = student.Session.Name,
            BatchName = student.Batch?.BatchName ?? "Not Assigned"
        };

        await PopulateSelectListsAsync(viewModel);
        return viewModel;
    }

    public async Task<StudentViewModel?> GetStudentByIdForTeacherAsync(int id, string teacherId)
    {
        // First, get the teacher by user ID to get teacher ID
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == teacherId && !t.IsDeleted);
        
        if (teacher == null)
            return null;

        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .ThenInclude(b => b!.PrimaryInstructor)
            .Include(s => s.Batch)
            .ThenInclude(b => b!.SecondaryInstructor)
            .FirstOrDefaultAsync(s => s.Id == id && 
                               !s.IsDeleted && 
                               s.Batch != null && 
                               (s.Batch.PrimaryInstructorId == teacher.Id || s.Batch.SecondaryInstructorId == teacher.Id));

        if (student == null) return null;

        return await GetStudentByIdAsync(id); // Reuse existing method for full details
    }

    public async Task<StudentViewModel?> GetStudentProfileAsync(string userId)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

        if (student == null) return null;

        return await GetStudentByIdAsync(student.Id); // Reuse existing method for full details
    }

    public async Task<StudentViewModel?> GetStudentByRegistrationNumberAsync(string registrationNumber)
    {
        var student = await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.RegistrationNumber == registrationNumber && !s.IsDeleted);

        if (student == null) return null;

        return await GetStudentByIdAsync(student.Id);
    }

    public async Task<bool> CreateStudentAsync(StudentViewModel model, string? photoPath = null)
    {
        try
        {
            // Check if registration number is unique
            if (!await IsRegistrationNumberUniqueAsync(model.RegistrationNumber))
            {
                return false;
            }

            var student = new Student
            {
                RegistrationNumber = model.RegistrationNumber,
                StudentCode = model.RegistrationNumber, // Using RegistrationNumber as StudentCode
                FirstName = model.FirstName,
                LastName = model.LastName,
                FatherName = model.FatherName,
                CNIC = model.CNIC,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                BloodGroup = model.BloodGroup,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                City = model.City,
                Province = model.Province,
                PostalCode = model.PostalCode,
                EmergencyContactName = model.EmergencyContactName,
                EmergencyContactPhone = model.EmergencyContactPhone,
                PhotoPath = photoPath ?? model.PhotoPath,
                AdmissionDate = model.AdmissionDate,
                Status = model.Status,
                TradeId = model.TradeId,
                SessionId = model.SessionId,
                BatchId = model.BatchId,
                TimingId = model.TimingId,
                PreviousQualification = model.PreviousQualification,
                PreviousInstitute = model.PreviousInstitute,
                PreviousMarks = model.PreviousMarks,
                TotalFee = model.TotalFee,
                PaidAmount = model.PaidAmount,
                Remarks = model.Remarks,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return false;
        }
    }

    public async Task<bool> UpdateStudentAsync(StudentViewModel model, string? photoPath = null)
    {
        try
        {
            var student = await _context.Students.FindAsync(model.Id);
            if (student == null) return false;

            // Check if registration number is unique (excluding current student)
            if (!await IsRegistrationNumberUniqueAsync(model.RegistrationNumber, model.Id))
            {
                return false;
            }

            student.RegistrationNumber = model.RegistrationNumber;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.FatherName = model.FatherName;
            student.CNIC = model.CNIC;
            student.DateOfBirth = model.DateOfBirth;
            student.Gender = model.Gender;
            student.BloodGroup = model.BloodGroup;
            student.PhoneNumber = model.PhoneNumber;
            student.Email = model.Email;
            student.Address = model.Address;
            student.City = model.City;
            student.Province = model.Province;
            student.PostalCode = model.PostalCode;
            student.EmergencyContactName = model.EmergencyContactName;
            student.EmergencyContactPhone = model.EmergencyContactPhone;
            student.AdmissionDate = model.AdmissionDate;
            student.Status = model.Status;
            student.TradeId = model.TradeId;
            student.SessionId = model.SessionId;
            student.BatchId = model.BatchId;
            student.TimingId = model.TimingId;
            student.PreviousQualification = model.PreviousQualification;
            student.PreviousInstitute = model.PreviousInstitute;
            student.PreviousMarks = model.PreviousMarks;
            student.TotalFee = model.TotalFee;
            student.PaidAmount = model.PaidAmount;
            student.Remarks = model.Remarks;
            student.ModifiedDate = DateTime.Now;

            if (!string.IsNullOrEmpty(photoPath))
            {
                student.PhotoPath = photoPath;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student");
            return false;
        }
    }

    public async Task<bool> DeleteStudentAsync(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            student.IsDeleted = true;
            student.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student");
            return false;
        }
    }

    public async Task<bool> IsRegistrationNumberUniqueAsync(string registrationNumber, int? excludeId = null)
    {
        var query = _context.Students.Where(s => s.RegistrationNumber == registrationNumber && !s.IsDeleted);
        
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsCnicUniqueAsync(string cnic, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(cnic)) return true;

        var query = _context.Students.Where(s => s.CNIC == cnic && !s.IsDeleted);
        
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<string> GenerateRegistrationNumberAsync(int tradeId, int sessionId)
    {
        try
        {
            var trade = await _context.Trades.FindAsync(tradeId);
            var session = await _context.Sessions.FindAsync(sessionId);

            if (trade == null || session == null)
            {
                return string.Empty;
            }

            // Get trade code (first 3 letters of trade name)
            var tradeCode = new string(trade.NameEnglish.Take(3).ToArray()).ToUpper();
            
            // Get session year (last 2 digits of start year)
            var sessionYear = session.StartDate.ToString("yy");

            // Get next sequence number for this trade and session
            var existingCount = await _context.Students
                .Where(s => s.TradeId == tradeId && s.SessionId == sessionId && !s.IsDeleted)
                .CountAsync();

            var sequence = (existingCount + 1).ToString("D4");

            return $"{tradeCode}{sessionYear}{sequence}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating registration number");
            return string.Empty;
        }
    }

    public async Task<decimal> CalculateRemainingFeeAsync(int studentId)
    {
        var student = await _context.Students.FindAsync(studentId);
        if (student == null) return 0;

        return student.TotalFee - student.PaidAmount;
    }

    public async Task<bool> AssignToBatchAsync(int studentId, int batchId)
    {
        try
        {
            var student = await _context.Students.FindAsync(studentId);
            var batch = await _context.Batches.FindAsync(batchId);

            if (student == null || batch == null) return false;

            // Check if batch is full
            var currentEnrollment = await _context.Students
                .CountAsync(s => s.BatchId == batchId && !s.IsDeleted);

            if (currentEnrollment >= batch.MaxStudents)
            {
                return false;
            }

            student.BatchId = batchId;
            student.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning student to batch");
            return false;
        }
    }

    public async Task<bool> RemoveFromBatchAsync(int studentId)
    {
        try
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return false;

            student.BatchId = null;
            student.TimingId = null;
            student.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing student from batch");
            return false;
        }
    }

    public async Task<List<StudentViewModel>> GetStudentsByBatchAsync(int batchId)
    {
        return await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .Where(s => s.BatchId == batchId && !s.IsDeleted)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name,
                BatchName = s.Batch != null ? s.Batch.BatchName : ""
            })
            .ToListAsync();
    }

    public async Task<List<StudentViewModel>> GetStudentsByTradeAsync(int tradeId)
    {
        return await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Where(s => s.TradeId == tradeId && !s.IsDeleted)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name
            })
            .ToListAsync();
    }

    public async Task<List<StudentViewModel>> GetStudentsBySessionAsync(int sessionId)
    {
        return await _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Where(s => s.SessionId == sessionId && !s.IsDeleted)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name
            })
            .ToListAsync();
    }

    public async Task<int> GetTotalStudentsCountAsync()
    {
        return await _context.Students.CountAsync(s => !s.IsDeleted);
    }

    public async Task<int> GetActiveStudentsCountAsync()
    {
        return await _context.Students.CountAsync(s => s.Status == "Active" && !s.IsDeleted);
    }

    public async Task<Dictionary<string, int>> GetStudentsCountByStatusAsync()
    {
        return await _context.Students
            .Where(s => !s.IsDeleted)
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetStudentsCountByTradeAsync()
    {
        return await _context.Students
            .Include(s => s.Trade)
            .Where(s => !s.IsDeleted)
            .GroupBy(s => s.Trade.NameEnglish)
            .Select(g => new { Trade = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Trade, x => x.Count);
    }

    public async Task<bool> ProvisionStudentLoginAsync(int studentId)
    {
        try
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || !string.IsNullOrEmpty(student.UserId))
            {
                return false; // Student not found or already has a login
            }

            // Create user account
            var user = new ApplicationUser
            {
                UserName = student.RegistrationNumber,
                Email = student.Email ?? $"{student.RegistrationNumber}@student.local",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "Sostti123+");
            if (!result.Succeeded)
            {
                return false;
            }

            // Assign Student role
            await _userManager.AddToRoleAsync(user, UserRoles.Student);

            // Link user to student
            student.UserId = user.Id;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning student login");
            return false;
        }
    }

    public async Task<(int created, int skipped, int failed)> ProvisionAllUnlinkedStudentLoginsAsync()
    {
        int created = 0, skipped = 0, failed = 0;

        var unlinkedStudents = await _context.Students
            .Where(s => s.UserId == null && !s.IsDeleted)
            .ToListAsync();

        foreach (var student in unlinkedStudents)
        {
            try
            {
                var success = await ProvisionStudentLoginAsync(student.Id);
                if (success)
                    created++;
                else
                    skipped++;
            }
            catch
            {
                failed++;
            }
        }

        return (created, skipped, failed);
    }

    public async Task<List<BatchTimingViewModel>> GetBatchTimingsAsync(int batchId)
    {
        var batchTimings = await _context.BatchTimings
            .Include(bt => bt.Timing)
            .Where(bt => bt.BatchId == batchId && !bt.IsDeleted)
            .ToListAsync();

        var result = new List<BatchTimingViewModel>();

        foreach (var bt in batchTimings)
        {
            // Count current students enrolled in this batch with this timing
            var currentStudents = await _context.Students
                .CountAsync(s => s.BatchId == batchId && s.TimingId == bt.TimingId && !s.IsDeleted);

            result.Add(new BatchTimingViewModel
            {
                Id = bt.Id,
                BatchId = bt.BatchId,
                TimingId = bt.TimingId,
                TimingName = bt.Timing.Name,
                TimingDescription = $"{bt.Timing.StartTime:hh\\:mm} - {bt.Timing.EndTime:hh\\:mm} ({bt.Timing.Name})",
                MaxStudents = bt.MaxStudents,
                CurrentStudents = currentStudents
            });
        }

        return result;
    }

    public async Task<dynamic?> GetBatchByIdAsync(int batchId)
    {
        var batch = await _context.Batches
            .Where(b => b.Id == batchId && !b.IsDeleted)
            .Select(b => new
            {
                b.Id,
                b.BatchCode,
                b.BatchName,
                b.TimingId,
                b.MaxStudents,
                b.CurrentEnrollment
            })
            .FirstOrDefaultAsync();

        return batch;
    }

    public async Task<dynamic?> GetTimingByIdAsync(int timingId)
    {
        var timing = await _context.Timings
            .Where(t => t.Id == timingId && !t.IsDeleted)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.StartTime,
                t.EndTime
            })
            .FirstOrDefaultAsync();

        return timing;
    }

    public async Task<(int created, int updated, int failed, List<string> errors)> ImportFromCsvAsync(Stream csvStream)
    {
        int created = 0, updated = 0, failed = 0;
        var errors = new List<string>();

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            
            // Read header line
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                errors.Add("CSV file is empty or header is missing");
                return (0, 0, 1, errors);
            }

            int lineNumber = 1;
            
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = line.Split(',');
                    
                    // Expected CSV format:
                    // RegistrationNumber,FirstName,LastName,FatherName,CNIC,DateOfBirth,Gender,PhoneNumber,Email,TradeId,SessionId,TotalFee,Status
                    
                    if (values.Length < 13)
                    {
                        errors.Add($"Line {lineNumber}: Insufficient columns");
                        failed++;
                        continue;
                    }

                    var registrationNumber = values[0].Trim();
                    
                    // Check if student already exists
                    var existingStudent = await _context.Students
                        .FirstOrDefaultAsync(s => s.RegistrationNumber == registrationNumber && !s.IsDeleted);

                    if (existingStudent != null)
                    {
                        // Update existing student
                        existingStudent.FirstName = values[1].Trim();
                        existingStudent.LastName = values[2].Trim();
                        existingStudent.FatherName = values[3].Trim();
                        existingStudent.CNIC = values[4].Trim();
                        
                        if (DateTime.TryParseExact(values[5].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                        {
                            existingStudent.DateOfBirth = dob;
                        }
                        
                        existingStudent.Gender = values[6].Trim();
                        existingStudent.PhoneNumber = values[7].Trim();
                        existingStudent.Email = values[8].Trim();
                        
                        if (int.TryParse(values[9].Trim(), out var tradeId))
                            existingStudent.TradeId = tradeId;
                            
                        if (int.TryParse(values[10].Trim(), out var sessionId))
                            existingStudent.SessionId = sessionId;
                            
                        if (decimal.TryParse(values[11].Trim(), out var totalFee))
                            existingStudent.TotalFee = totalFee;
                            
                        existingStudent.Status = values[12].Trim();
                        existingStudent.ModifiedDate = DateTime.Now;
                        
                        updated++;
                    }
                    else
                    {
                        // Create new student
                        var student = new Student
                        {
                            RegistrationNumber = registrationNumber,
                            StudentCode = registrationNumber,
                            FirstName = values[1].Trim(),
                            LastName = values[2].Trim(),
                            FatherName = values[3].Trim(),
                            CNIC = values[4].Trim(),
                            Gender = values[6].Trim(),
                            PhoneNumber = values[7].Trim(),
                            Email = values[8].Trim(),
                            Status = values[12].Trim(),
                            AdmissionDate = DateTime.Today,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now
                        };

                        if (DateTime.TryParseExact(values[5].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                        {
                            student.DateOfBirth = dob;
                        }
                        
                        if (int.TryParse(values[9].Trim(), out var tradeId))
                            student.TradeId = tradeId;
                        else
                        {
                            errors.Add($"Line {lineNumber}: Invalid TradeId");
                            failed++;
                            continue;
                        }
                            
                        if (int.TryParse(values[10].Trim(), out var sessionId))
                            student.SessionId = sessionId;
                        else
                        {
                            errors.Add($"Line {lineNumber}: Invalid SessionId");
                            failed++;
                            continue;
                        }
                            
                        if (decimal.TryParse(values[11].Trim(), out var totalFee))
                            student.TotalFee = totalFee;

                        _context.Students.Add(student);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNumber}: {ex.Message}");
                    failed++;
                }
            }

            if (created > 0 || updated > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV");
            errors.Add($"General error: {ex.Message}");
            failed++;
        }

        return (created, updated, failed, errors);
    }

    private async Task PopulateSelectListsAsync(StudentViewModel model)
    {
        var trades = await _context.Trades
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish })
            .ToListAsync();

        var sessions = await _context.Sessions
            .Where(s => s.IsActive && !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        var batches = await _context.Batches
            .Where(b => b.TradeId == model.TradeId && b.SessionId == model.SessionId && b.Status == "Active" && !b.IsDeleted)
            .Select(b => new { b.Id, b.BatchName })
            .ToListAsync();

        var genderOptions = new List<SelectListItem>
        {
            new() { Value = "Male", Text = "Male" },
            new() { Value = "Female", Text = "Female" }
        };

        var statusOptions = new List<SelectListItem>
        {
            new() { Value = "Active", Text = "Active" },
            new() { Value = "Graduated", Text = "Graduated" },
            new() { Value = "Dropped", Text = "Dropped" },
            new() { Value = "Suspended", Text = "Suspended" }
        };

        model.Trades = new SelectList(trades, "Id", "NameEnglish", model.TradeId);
        model.Sessions = new SelectList(sessions, "Id", "Name", model.SessionId);
        model.Batches = new SelectList(batches, "Id", "BatchName", model.BatchId);
        model.GenderOptions = new SelectList(genderOptions, "Value", "Text", model.Gender);
        model.StatusOptions = new SelectList(statusOptions, "Value", "Text", model.Status);
    }
}
