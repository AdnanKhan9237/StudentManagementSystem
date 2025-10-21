using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;

namespace StudentManagementSystem.Services;

public interface IRoomCapacityService
{
    Task<RoomUtilizationSummary> GetRoomUtilizationAsync(int roomId);
    Task<List<RoomTimeSlotAvailability>> GetAvailableTimeSlotsAsync(int roomId);
    Task<RoomCapacityReport> GetRoomCapacityReportAsync();
    Task<bool> IsRoomAvailableAsync(int roomId, int timingId, int? excludeBatchId = null);
}

public class RoomCapacityService : IRoomCapacityService
{
    private readonly ApplicationDbContext _context;

    public RoomCapacityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomUtilizationSummary> GetRoomUtilizationAsync(int roomId)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null)
            throw new ArgumentException("Room not found", nameof(roomId));

        var activeBatches = await _context.Batches
            .Include(b => b.Timing)
            .Include(b => b.Trade)
            .Include(b => b.Students)
            .Where(b => b.RoomId == roomId && !b.IsDeleted && b.Status == "Active")
            .ToListAsync();

        var totalTimings = await _context.Timings
            .Where(t => !t.IsDeleted && t.IsActive)
            .CountAsync();

        var utilizedTimings = activeBatches.Count;
        var totalCurrentStudents = activeBatches.Sum(b => b.Students.Count(s => !s.IsDeleted));
        var totalMaxCapacity = activeBatches.Sum(b => b.MaxStudents);
        var dailyMaxCapacity = room.Capacity * totalTimings;

        return new RoomUtilizationSummary
        {
            RoomId = roomId,
            RoomNumber = room.RoomNumber,
            RoomName = room.RoomName,
            RoomCapacity = room.Capacity,
            TotalTimings = totalTimings,
            UtilizedTimings = utilizedTimings,
            AvailableTimings = totalTimings - utilizedTimings,
            UtilizationPercentage = totalTimings > 0 ? (double)utilizedTimings / totalTimings * 100 : 0,
            CurrentStudents = totalCurrentStudents,
            MaxPossibleStudents = totalMaxCapacity,
            DailyMaxCapacity = dailyMaxCapacity,
            CapacityUtilization = dailyMaxCapacity > 0 ? (double)totalMaxCapacity / dailyMaxCapacity * 100 : 0,
            ActiveBatches = activeBatches.Select(b => new BatchUtilizationInfo
            {
                BatchId = b.Id,
                BatchCode = b.BatchCode,
                BatchName = b.BatchName,
                TradeName = b.Trade.NameEnglish,
                TimingSlot = b.Timing != null ? $"{b.Timing.StartTime:HH:mm} - {b.Timing.EndTime:HH:mm}" : "Not Assigned",
                MaxStudents = b.MaxStudents,
                CurrentStudents = b.Students.Count(s => !s.IsDeleted),
                CapacityUsed = b.MaxStudents > 0 ? (double)b.Students.Count(s => !s.IsDeleted) / b.MaxStudents * 100 : 0
            }).ToList()
        };
    }

    public async Task<List<RoomTimeSlotAvailability>> GetAvailableTimeSlotsAsync(int roomId)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null)
            return new List<RoomTimeSlotAvailability>();

        var allTimings = await _context.Timings
            .Where(t => !t.IsDeleted && t.IsActive)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        var occupiedTimingIds = await _context.Batches
            .Where(b => b.RoomId == roomId && !b.IsDeleted && b.Status == "Active" && b.TimingId.HasValue)
            .Select(b => b.TimingId!.Value)
            .ToListAsync();

        return allTimings.Select(t => new RoomTimeSlotAvailability
        {
            TimingId = t.Id,
            TimingName = t.Name,
            TimeSlot = $"{t.StartTime:HH:mm} - {t.EndTime:HH:mm}",
            IsAvailable = !occupiedTimingIds.Contains(t.Id),
            RoomCapacity = room.Capacity,
            OccupiedBy = occupiedTimingIds.Contains(t.Id) ? 
                _context.Batches
                    .Where(b => b.RoomId == roomId && b.TimingId == t.Id && !b.IsDeleted && b.Status == "Active")
                    .Select(b => b.BatchCode)
                    .FirstOrDefault() : null
        }).ToList();
    }

    public async Task<RoomCapacityReport> GetRoomCapacityReportAsync()
    {
        var rooms = await _context.Rooms
            .Where(r => !r.IsDeleted)
            .ToListAsync();

        var totalTimings = await _context.Timings
            .Where(t => !t.IsDeleted && t.IsActive)
            .CountAsync();

        var roomReports = new List<RoomSummary>();

        foreach (var room in rooms)
        {
            var activeBatches = await _context.Batches
                .Include(b => b.Students)
                .Where(b => b.RoomId == room.Id && !b.IsDeleted && b.Status == "Active")
                .ToListAsync();

            var utilizedTimings = activeBatches.Count;
            var currentStudents = activeBatches.Sum(b => b.Students.Count(s => !s.IsDeleted));
            var maxCapacity = activeBatches.Sum(b => b.MaxStudents);
            var dailyMaxCapacity = room.Capacity * totalTimings;

            roomReports.Add(new RoomSummary
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                RoomName = room.RoomName,
                Capacity = room.Capacity,
                UtilizedTimings = utilizedTimings,
                AvailableTimings = totalTimings - utilizedTimings,
                CurrentStudents = currentStudents,
                MaxPossibleStudents = maxCapacity,
                DailyMaxCapacity = dailyMaxCapacity,
                UtilizationPercentage = totalTimings > 0 ? (double)utilizedTimings / totalTimings * 100 : 0,
                CapacityPercentage = dailyMaxCapacity > 0 ? (double)maxCapacity / dailyMaxCapacity * 100 : 0
            });
        }

        return new RoomCapacityReport
        {
            TotalRooms = rooms.Count,
            TotalTimings = totalTimings,
            TotalDailyCapacity = rooms.Sum(r => r.Capacity * totalTimings),
            TotalCurrentStudents = roomReports.Sum(r => r.CurrentStudents),
            TotalMaxCapacity = roomReports.Sum(r => r.MaxPossibleStudents),
            OverallUtilization = rooms.Count > 0 ? roomReports.Average(r => r.UtilizationPercentage) : 0,
            OverallCapacityUsage = roomReports.Sum(r => r.DailyMaxCapacity) > 0 ? 
                (double)roomReports.Sum(r => r.MaxPossibleStudents) / roomReports.Sum(r => r.DailyMaxCapacity) * 100 : 0,
            RoomSummaries = roomReports
        };
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, int timingId, int? excludeBatchId = null)
    {
        var query = _context.Batches
            .Where(b => b.RoomId == roomId && 
                       b.TimingId == timingId && 
                       !b.IsDeleted && 
                       b.Status == "Active");

        if (excludeBatchId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBatchId.Value);
        }

        return !await query.AnyAsync();
    }
}

// Data Transfer Objects
public class RoomUtilizationSummary
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public int RoomCapacity { get; set; }
    public int TotalTimings { get; set; }
    public int UtilizedTimings { get; set; }
    public int AvailableTimings { get; set; }
    public double UtilizationPercentage { get; set; }
    public int CurrentStudents { get; set; }
    public int MaxPossibleStudents { get; set; }
    public int DailyMaxCapacity { get; set; }
    public double CapacityUtilization { get; set; }
    public List<BatchUtilizationInfo> ActiveBatches { get; set; } = new();
}

public class BatchUtilizationInfo
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string TimingSlot { get; set; } = string.Empty;
    public int MaxStudents { get; set; }
    public int CurrentStudents { get; set; }
    public double CapacityUsed { get; set; }
}

public class RoomTimeSlotAvailability
{
    public int TimingId { get; set; }
    public string TimingName { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int RoomCapacity { get; set; }
    public string? OccupiedBy { get; set; }
}

public class RoomCapacityReport
{
    public int TotalRooms { get; set; }
    public int TotalTimings { get; set; }
    public int TotalDailyCapacity { get; set; }
    public int TotalCurrentStudents { get; set; }
    public int TotalMaxCapacity { get; set; }
    public double OverallUtilization { get; set; }
    public double OverallCapacityUsage { get; set; }
    public List<RoomSummary> RoomSummaries { get; set; } = new();
}

public class RoomSummary
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public int Capacity { get; set; }
    public int UtilizedTimings { get; set; }
    public int AvailableTimings { get; set; }
    public int CurrentStudents { get; set; }
    public int MaxPossibleStudents { get; set; }
    public int DailyMaxCapacity { get; set; }
    public double UtilizationPercentage { get; set; }
    public double CapacityPercentage { get; set; }
}