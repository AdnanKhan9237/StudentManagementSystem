using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;

namespace StudentManagementSystem;

public static class BatchCodeChecker
{
    public static async Task CheckBatchCodeAsync(ApplicationDbContext context, string batchCode)
    {
        try
        {
            var batches = await context.Batches
                .Where(b => b.BatchCode == batchCode)
                .Select(b => new { b.Id, b.BatchCode, b.BatchName, b.IsDeleted, b.CreatedDate, b.CreatedBy })
                .ToListAsync();

            Console.WriteLine($"Checking for BatchCode: {batchCode}");
            Console.WriteLine($"Found {batches.Count} records:");
            
            foreach (var batch in batches)
            {
                Console.WriteLine($"  ID: {batch.Id}, Name: {batch.BatchName}, IsDeleted: {batch.IsDeleted}, Created: {batch.CreatedDate:yyyy-MM-dd HH:mm:ss}, By: {batch.CreatedBy}");
            }

            if (batches.Count > 1)
            {
                Console.WriteLine("WARNING: Multiple records found with the same batch code!");
            }
            else if (batches.Count == 0)
            {
                Console.WriteLine("No records found with this batch code.");
            }
            else
            {
                Console.WriteLine("Single record found - no duplicates detected.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking batch code: {ex.Message}");
        }
    }
}