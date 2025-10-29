using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Interfaces;

public interface IStudentService
{
    Task<StudentListViewModel> GetStudentsAsync(int pageNumber, int pageSize, string searchTerm = "", 
        int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "");
    
    // Role-based methods for filtering
    Task<StudentListViewModel> GetStudentsForTeacherAsync(string teacherId, int pageNumber, int pageSize, 
        string searchTerm = "", int? tradeFilter = null, int? sessionFilter = null, string statusFilter = "");
        
    Task<StudentViewModel?> GetStudentByIdAsync(int id);
    Task<StudentViewModel?> GetStudentByIdForTeacherAsync(int id, string teacherId);
    Task<StudentViewModel?> GetStudentProfileAsync(string userId);
    Task<StudentViewModel?> GetStudentByRegistrationNumberAsync(string registrationNumber);
    
    Task<bool> CreateStudentAsync(StudentViewModel model, string? photoPath = null);
    Task<bool> UpdateStudentAsync(StudentViewModel model, string? photoPath = null);
    Task<bool> DeleteStudentAsync(int id);
    
    Task<bool> IsRegistrationNumberUniqueAsync(string registrationNumber, int? excludeId = null);
    Task<bool> IsCnicUniqueAsync(string cnic, int? excludeId = null);
    
    Task<string> GenerateRegistrationNumberAsync(int tradeId, int sessionId);
    
    Task<decimal> CalculateRemainingFeeAsync(int studentId);
    Task<bool> AssignToBatchAsync(int studentId, int batchId);
    Task<bool> RemoveFromBatchAsync(int studentId);
    
    Task<List<StudentViewModel>> GetStudentsByBatchAsync(int batchId);
    Task<List<StudentViewModel>> GetStudentsByTradeAsync(int tradeId);
    Task<List<StudentViewModel>> GetStudentsBySessionAsync(int sessionId);
    Task<int> GetTotalStudentsCountAsync();

    // Provision Identity login for a student with default password and Student role
    Task<bool> ProvisionStudentLoginAsync(int studentId);

    // Bulk provision for all students without linked user accounts
    Task<(int created, int skipped, int failed)> ProvisionAllUnlinkedStudentLoginsAsync();

    Task<int> GetActiveStudentsCountAsync();
    Task<Dictionary<string, int>> GetStudentsCountByStatusAsync();
    Task<Dictionary<string, int>> GetStudentsCountByTradeAsync();
    
    // Batch timing methods
    Task<List<BatchTimingViewModel>> GetBatchTimingsAsync(int batchId);
    Task<dynamic?> GetBatchByIdAsync(int batchId);
    Task<dynamic?> GetTimingByIdAsync(int timingId);
    
    // CSV Import
    Task<(int created, int updated, int failed, List<string> errors)> ImportFromCsvAsync(Stream csvStream);
}
