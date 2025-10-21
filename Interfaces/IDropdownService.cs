using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementSystem.Interfaces;

public interface IDropdownService
{
    Task<SelectList> GetTradesAsync(int? selectedValue = null);
    Task<SelectList> GetSessionsAsync(int? selectedValue = null);
    Task<SelectList> GetBatchesAsync(int? tradeId = null, int? sessionId = null, int? selectedValue = null);
    Task<SelectList> GetRoomsAsync(int? selectedValue = null);
    Task<SelectList> GetTimingsAsync(int? selectedValue = null);
    Task<SelectList> GetInstructorsAsync(int? selectedValue = null);
    SelectList GetGenderOptions(string? selectedValue = null);
    SelectList GetStatusOptions(string? selectedValue = null);
    SelectList GetBloodGroupOptions(string? selectedValue = null);
    SelectList GetProvinceOptions(string? selectedValue = null);
    SelectList GetQualificationOptions(string? selectedValue = null);
}