using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentManagementSystem.Data;
using StudentManagementSystem.Interfaces;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentService> _logger;

    public StudentService(ApplicationDbContext context, ILogger<StudentService> logger)
    {
        _context = context;
        _logger = logger;
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
                PhotoPath = photoPath,
                AdmissionDate = model.AdmissionDate,
                Status = model.Status,
                TradeId = model.TradeId,
                SessionId = model.SessionId,
                BatchId = model.BatchId,
                PreviousQualification = model.PreviousQualification,
                PreviousInstitute = model.PreviousInstitute,
                PreviousMarks = model.PreviousMarks,
                TotalFee = model.TotalFee,
                PaidAmount = model.PaidAmount,
                Remarks = model.Remarks,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
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
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == model.Id);
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
            
            if (!string.IsNullOrEmpty(photoPath))
            {
                student.PhotoPath = photoPath;
            }
            
            student.AdmissionDate = model.AdmissionDate;
            student.Status = model.Status;
            student.TradeId = model.TradeId;
            student.SessionId = model.SessionId;
            student.BatchId = model.BatchId;
            student.PreviousQualification = model.PreviousQualification;
            student.PreviousInstitute = model.PreviousInstitute;
            student.PreviousMarks = model.PreviousMarks;
            student.TotalFee = model.TotalFee;
            student.PaidAmount = model.PaidAmount;
            student.Remarks = model.Remarks;
            student.ModifiedDate = DateTime.UtcNow;

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
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return false;

            student.IsDeleted = true;
            student.ModifiedDate = DateTime.UtcNow;

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
        var trade = await _context.Trades.FirstOrDefaultAsync(t => t.Id == tradeId);
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        
        if (trade == null || session == null) return "";

        var year = session.StartDate.Year.ToString().Substring(2);
        var tradeCode = trade.Code;
        
        // Get the next sequence number for this trade and session
        var lastStudent = await _context.Students
            .Where(s => s.TradeId == tradeId && s.SessionId == sessionId && !s.IsDeleted)
            .OrderByDescending(s => s.RegistrationNumber)
            .FirstOrDefaultAsync();

        int sequenceNumber = 1;
        if (lastStudent != null)
        {
            // Extract sequence number from last registration number
            var lastRegNumber = lastStudent.RegistrationNumber;
            var parts = lastRegNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int lastSequence))
            {
                sequenceNumber = lastSequence + 1;
            }
        }

        return $"{year}-{tradeCode}-{sequenceNumber:D3}";
    }

    public async Task<decimal> CalculateRemainingFeeAsync(int studentId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        return student?.RemainingAmount ?? 0;
    }

    public async Task<bool> AssignToBatchAsync(int studentId, int batchId)
    {
        try
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            var batch = await _context.Batches.FirstOrDefaultAsync(b => b.Id == batchId);
            
            if (student == null || batch == null) return false;

            // Check if batch has available slots
            var currentEnrollment = await _context.Students.CountAsync(s => s.BatchId == batchId && !s.IsDeleted);
            if (currentEnrollment >= batch.MaxStudents) return false;

            student.BatchId = batchId;
            student.ModifiedDate = DateTime.UtcNow;

            // Update batch enrollment count
            batch.CurrentEnrollment = currentEnrollment + 1;

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
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return false;

            var batchId = student.BatchId;
            student.BatchId = null;
            student.ModifiedDate = DateTime.UtcNow;

            // Update batch enrollment count
            if (batchId.HasValue)
            {
                var batch = await _context.Batches.FirstOrDefaultAsync(b => b.Id == batchId.Value);
                if (batch != null)
                {
                    var currentEnrollment = await _context.Students.CountAsync(s => s.BatchId == batchId && !s.IsDeleted);
                    batch.CurrentEnrollment = Math.Max(0, currentEnrollment - 1);
                }
            }

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
            .Where(s => s.BatchId == batchId && !s.IsDeleted)
            .Select(s => new StudentViewModel
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Status = s.Status,
                TradeName = s.Trade.NameEnglish,
                SessionName = s.Session.Name
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
                Status = s.Status,
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
                Status = s.Status,
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
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetStudentsCountByTradeAsync()
    {
        return await _context.Students
            .Include(s => s.Trade)
            .Where(s => !s.IsDeleted)
            .GroupBy(s => s.Trade.NameEnglish)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    // Role-based methods
    public async Task<StudentListViewModel> GetStudentsForTeacherAsync(string teacherId, int pageNumber, int pageSize, 
        string searchTerm = "", int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "")
    {
        // First, get the teacher by user ID to get teacher ID
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == teacherId && !t.IsDeleted);
        
        if (teacher == null)
            return new StudentListViewModel();

        var query = _context.Students
            .Include(s => s.Trade)
            .Include(s => s.Session)
            .Include(s => s.Batch)
            .ThenInclude(b => b!.PrimaryInstructor)
            .Include(s => s.Batch)
            .ThenInclude(b => b!.SecondaryInstructor)
            .Where(s => !s.IsDeleted && 
                   s.Batch != null && 
                   (s.Batch.PrimaryInstructorId == teacher.Id || s.Batch.SecondaryInstructorId == teacher.Id))
            .AsQueryable();

        // Apply filters (same as regular GetStudentsAsync)
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