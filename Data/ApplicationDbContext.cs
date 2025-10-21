using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Trade> Trades { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Timing> Timings { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<FeeTransaction> FeeTransactions { get; set; }
    public DbSet<FeePayment> FeePayments { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<BatchStudent> BatchStudents { get; set; }
    public DbSet<FeeStructure> FeeStructures { get; set; }
    public DbSet<StudentFeeAdjustment> StudentFeeAdjustments { get; set; }
    public DbSet<Teacher> Teachers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure decimal precision for financial fields
        builder.Entity<Trade>()
            .Property(t => t.TotalFee)
            .HasPrecision(18, 2);

        builder.Entity<Student>()
            .Property(s => s.TotalFee)
            .HasPrecision(18, 2);

        builder.Entity<Student>()
            .Property(s => s.PaidAmount)
            .HasPrecision(18, 2);

        builder.Entity<Student>()
            .Property(s => s.PreviousMarks)
            .HasPrecision(5, 2);

        builder.Entity<Exam>()
            .Property(e => e.TotalMarks)
            .HasPrecision(10, 2);

        builder.Entity<Exam>()
            .Property(e => e.PassingMarks)
            .HasPrecision(10, 2);

        builder.Entity<ExamResult>()
            .Property(er => er.ObtainedMarks)
            .HasPrecision(10, 2);

        builder.Entity<ExamResult>()
            .Property(er => er.TotalMarks)
            .HasPrecision(10, 2);

        builder.Entity<FeeTransaction>()
            .Property(ft => ft.Amount)
            .HasPrecision(18, 2);

        builder.Entity<FeePayment>()
            .Property(fp => fp.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Certificate>()
            .Property(c => c.ObtainedMarks)
            .HasPrecision(10, 2);

        builder.Entity<Certificate>()
            .Property(c => c.TotalMarks)
            .HasPrecision(10, 2);

        // Configure decimal precision for new fee entities
        builder.Entity<FeeStructure>()
            .Property(fs => fs.MaleFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.FemaleFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.TransgenderFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.StandardFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.TuitionFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.AdmissionFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.SecurityFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.ExamFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.CertificateFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.LabFee)
            .HasPrecision(18, 2);

        builder.Entity<FeeStructure>()
            .Property(fs => fs.MaterialFee)
            .HasPrecision(18, 2);

        builder.Entity<StudentFeeAdjustment>()
            .Property(sfa => sfa.AdjustmentAmount)
            .HasPrecision(18, 2);

        builder.Entity<StudentFeeAdjustment>()
            .Property(sfa => sfa.AdjustmentPercentage)
            .HasPrecision(5, 2);
        
        builder.Entity<Teacher>()
            .Property(t => t.Salary)
            .HasPrecision(18, 2);

        // Configure indexes for performance
        builder.Entity<Student>()
            .HasIndex(s => s.RegistrationNumber)
            .IsUnique();

        builder.Entity<Student>()
            .HasIndex(s => s.CNIC);

        builder.Entity<FeeTransaction>()
            .HasIndex(ft => ft.TransactionNumber)
            .IsUnique();

        builder.Entity<FeePayment>()
            .HasIndex(fp => fp.ReceiptNumber)
            .IsUnique();

        builder.Entity<Certificate>()
            .HasIndex(c => c.CertificateNumber)
            .IsUnique();

        builder.Entity<Batch>()
            .HasIndex(b => b.BatchCode)
            .IsUnique();
        
        builder.Entity<Teacher>()
            .HasIndex(t => t.TeacherCode)
            .IsUnique();
        
        builder.Entity<Teacher>()
            .HasIndex(t => t.Username)
            .IsUnique();

        // Configure indexes for new fee entities
        builder.Entity<FeeStructure>()
            .HasIndex(fs => new { fs.TradeId, fs.SessionId, fs.EffectiveFrom });

        builder.Entity<StudentFeeAdjustment>()
            .HasIndex(sfa => new { sfa.StudentId, sfa.FeeStructureId });

        // Configure cascade delete behaviors
        builder.Entity<Student>()
            .HasOne(s => s.Batch)
            .WithMany()
            .HasForeignKey(s => s.BatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Attendance>()
            .HasOne(a => a.MarkedByUser)
            .WithMany()
            .HasForeignKey(a => a.MarkedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ExamResult>()
            .HasOne(er => er.EnteredByUser)
            .WithMany()
            .HasForeignKey(er => er.EnteredBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FeeTransaction>()
            .HasOne(ft => ft.ProcessedByUser)
            .WithMany()
            .HasForeignKey(ft => ft.ProcessedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Certificate>()
            .HasOne(c => c.IssuedByUser)
            .WithMany()
            .HasForeignKey(c => c.IssuedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Teacher-Batch relationships
        builder.Entity<Batch>()
            .HasOne(b => b.PrimaryInstructor)
            .WithMany(t => t.PrimaryBatches)
            .HasForeignKey(b => b.PrimaryInstructorId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.Entity<Batch>()
            .HasOne(b => b.SecondaryInstructor)
            .WithMany(t => t.SecondaryBatches)
            .HasForeignKey(b => b.SecondaryInstructorId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.Entity<Teacher>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Fix cascade delete cycles by setting some to NoAction
        builder.Entity<Student>()
            .HasOne(s => s.Session)
            .WithMany()
            .HasForeignKey(s => s.SessionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Student>()
            .HasOne(s => s.Trade)
            .WithMany()
            .HasForeignKey(s => s.TradeId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure relationships for new fee entities
        builder.Entity<FeeStructure>()
            .HasOne(fs => fs.Trade)
            .WithMany()
            .HasForeignKey(fs => fs.TradeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FeeStructure>()
            .HasOne(fs => fs.Session)
            .WithMany()
            .HasForeignKey(fs => fs.SessionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<StudentFeeAdjustment>()
            .HasOne(sfa => sfa.Student)
            .WithMany()
            .HasForeignKey(sfa => sfa.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentFeeAdjustment>()
            .HasOne(sfa => sfa.FeeStructure)
            .WithMany(fs => fs.StudentFeeAdjustments)
            .HasForeignKey(sfa => sfa.FeeStructureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
