using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Interfaces;

namespace StudentManagementSystem.Services;

public class DropdownService : IDropdownService
{
    private readonly ApplicationDbContext _context;

    public DropdownService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SelectList> GetTradesAsync(int? selectedValue = null)
    {
        var trades = await _context.Trades
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.NameEnglish })
            .ToListAsync();

        return new SelectList(trades, "Id", "NameEnglish", selectedValue);
    }

    public async Task<SelectList> GetSessionsAsync(int? selectedValue = null)
    {
        var sessions = await _context.Sessions
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.StartDate)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        return new SelectList(sessions, "Id", "Name", selectedValue);
    }

    public async Task<SelectList> GetBatchesAsync(int? tradeId = null, int? sessionId = null, int? selectedValue = null)
    {
        var query = _context.Batches.Where(b => b.Status == "Active" && !b.IsDeleted);

        if (tradeId.HasValue)
        {
            query = query.Where(b => b.TradeId == tradeId.Value);
        }

        if (sessionId.HasValue)
        {
            query = query.Where(b => b.SessionId == sessionId.Value);
        }

        var batches = await query
            .Select(b => new { b.Id, b.BatchName })
            .ToListAsync();

        return new SelectList(batches, "Id", "BatchName", selectedValue);
    }

    public async Task<SelectList> GetRoomsAsync(int? selectedValue = null)
    {
        var rooms = await _context.Rooms
            .Where(r => r.IsActive && !r.IsDeleted)
            .Select(r => new { r.Id, DisplayName = r.RoomNumber + " - " + r.RoomName })
            .ToListAsync();

        return new SelectList(rooms, "Id", "DisplayName", selectedValue);
    }

    public async Task<SelectList> GetTimingsAsync(int? selectedValue = null)
    {
        var timings = await _context.Timings
            .Where(t => t.IsActive && !t.IsDeleted)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        return new SelectList(timings, "Id", "Name", selectedValue);
    }

    public async Task<SelectList> GetInstructorsAsync(int? selectedValue = null)
    {
        var instructors = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, DisplayName = u.FirstName + " " + u.LastName })
            .ToListAsync();

        return new SelectList(instructors, "Id", "DisplayName", selectedValue);
    }

    public SelectList GetGenderOptions(string? selectedValue = null)
    {
        var genderOptions = new List<SelectListItem>
        {
            new() { Value = "Male", Text = "Male" },
            new() { Value = "Female", Text = "Female" },
            new() { Value = "Transgender", Text = "Transgender" }
        };

        return new SelectList(genderOptions, "Value", "Text", selectedValue);
    }

    public SelectList GetStatusOptions(string? selectedValue = null)
    {
        var statusOptions = new List<SelectListItem>
        {
            new() { Value = "Active", Text = "Active" },
            new() { Value = "Graduated", Text = "Graduated" },
            new() { Value = "Dropped", Text = "Dropped" },
            new() { Value = "Suspended", Text = "Suspended" }
        };

        return new SelectList(statusOptions, "Value", "Text", selectedValue);
    }

    public SelectList GetBloodGroupOptions(string? selectedValue = null)
    {
        var bloodGroups = new List<SelectListItem>
        {
            new() { Value = "A+", Text = "A+" },
            new() { Value = "A-", Text = "A-" },
            new() { Value = "B+", Text = "B+" },
            new() { Value = "B-", Text = "B-" },
            new() { Value = "O+", Text = "O+" },
            new() { Value = "O-", Text = "O-" },
            new() { Value = "AB+", Text = "AB+" },
            new() { Value = "AB-", Text = "AB-" }
        };

        return new SelectList(bloodGroups, "Value", "Text", selectedValue);
    }

    public SelectList GetProvinceOptions(string? selectedValue = null)
    {
        var provinces = new List<SelectListItem>
        {
            new() { Value = "Punjab", Text = "Punjab" },
            new() { Value = "Sindh", Text = "Sindh" },
            new() { Value = "KPK", Text = "Khyber Pakhtunkhwa" },
            new() { Value = "Balochistan", Text = "Balochistan" },
            new() { Value = "Gilgit-Baltistan", Text = "Gilgit-Baltistan" },
            new() { Value = "Azad Kashmir", Text = "Azad Kashmir" },
            new() { Value = "Islamabad", Text = "Islamabad Capital Territory" }
        };

        return new SelectList(provinces, "Value", "Text", selectedValue);
    }

    public SelectList GetQualificationOptions(string? selectedValue = null)
    {
        var qualifications = new List<SelectListItem>
        {
            new() { Value = "Matric", Text = "Matric" },
            new() { Value = "Intermediate", Text = "Intermediate" },
            new() { Value = "Bachelor", Text = "Bachelor" },
            new() { Value = "Master", Text = "Master" },
            new() { Value = "Other", Text = "Other" }
        };

        return new SelectList(qualifications, "Value", "Text", selectedValue);
    }
}