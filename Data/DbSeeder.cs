using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Models.Entities;

namespace StudentManagementSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

// Apply pending migrations and ensure database is up to date
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed SuperAdmin user only
        await SeedUsersAsync(userManager, context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.Teacher, UserRoles.Accounts, UserRoles.Student };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        // SuperAdmin - Only seed user
        await CreateUserIfNotExistsAsync(userManager, "superadmin@sms.com", "SuperAdmin@123", 
            "Super", "Admin", UserRoles.SuperAdmin, "IT", "System Administrator");
    }
    
    private static async Task<ApplicationUser?> CreateUserIfNotExistsAsync(
        UserManager<ApplicationUser> userManager, 
        string email, 
        string password, 
        string firstName, 
        string lastName, 
        string role,
        string? department = null,
        string? designation = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System",
                Department = department,
                Designation = designation
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                return user;
            }
        }
        return user;
    }

    private static async Task SeedTradesAsync(ApplicationDbContext context)
    {
        if (!await context.Trades.AnyAsync())
        {
            var trades = new List<Trade>
            {
                new Trade
                {
                    NameEnglish = "Computer Information Technology",
                    NameUrdu = "کمپیوٹر انفارمیشن ٹیکنالوجی",
                    Code = "CIT",
                    DescriptionEnglish = "Comprehensive computer and IT skills training",
                    DescriptionUrdu = "جامع کمپیوٹر اور آئی ٹی مہارات کی تربیت",
                    Duration = 12,
                    TotalFee = 25000,
                    MaxStudents = 30,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Electrical Technology",
                    NameUrdu = "برقی ٹیکنالوجی",
                    Code = "ELT",
                    DescriptionEnglish = "Electrical systems and maintenance training",
                    DescriptionUrdu = "برقی نظام اور مرمت کی تربیت",
                    Duration = 24,
                    TotalFee = 30000,
                    MaxStudents = 25,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Auto Mobile Technology",
                    NameUrdu = "آٹو موبائل ٹیکنالوجی",
                    Code = "AMT",
                    DescriptionEnglish = "Vehicle mechanics and repair training",
                    DescriptionUrdu = "گاڑیوں کی مکانیات اور مرمت کی تربیت",
                    Duration = 18,
                    TotalFee = 35000,
                    MaxStudents = 20,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Welding Technology",
                    NameUrdu = "ویلڈنگ ٹیکنالوجی",
                    Code = "WLD",
                    DescriptionEnglish = "Metal welding and fabrication skills",
                    DescriptionUrdu = "دھات کی ویلڈنگ اور تیاری کی مہارات",
                    Duration = 15,
                    TotalFee = 20000,
                    MaxStudents = 15,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Plumbing Technology",
                    NameUrdu = "پلمبنگ ٹیکنالوجی",
                    Code = "PLB",
                    DescriptionEnglish = "Water systems and pipe fitting training",
                    DescriptionUrdu = "پانی کے نظام اور پائپ فٹنگ کی تربیت",
                    Duration = 12,
                    TotalFee = 18000,
                    MaxStudents = 18,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Refrigeration & Air Conditioning",
                    NameUrdu = "ریفریجریشن اور ایئر کنڈیشننگ",
                    Code = "RAC",
                    DescriptionEnglish = "Cooling systems installation and repair",
                    DescriptionUrdu = "کولنگ سسٹم کی انسٹالیشن اور مرمت",
                    Duration = 18,
                    TotalFee = 32000,
                    MaxStudents = 20,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Electronics Technology",
                    NameUrdu = "الیکٹرانکس ٹیکنالوجی",
                    Code = "ELC",
                    DescriptionEnglish = "Electronic circuits and device repair",
                    DescriptionUrdu = "الیکٹرانک سرکٹس اور آلات کی مرمت",
                    Duration = 15,
                    TotalFee = 28000,
                    MaxStudents = 25,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Trade
                {
                    NameEnglish = "Civil Draftsmanship",
                    NameUrdu = "سول ڈرافٹسمین شپ",
                    Code = "CDD",
                    DescriptionEnglish = "Construction drawing and design",
                    DescriptionUrdu = "تعمیراتی ڈرائنگ اور ڈیزائن",
                    Duration = 12,
                    TotalFee = 22000,
                    MaxStudents = 30,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            context.Trades.AddRange(trades);
        }
    }

    private static async Task SeedSessionsAsync(ApplicationDbContext context)
    {
        if (!await context.Sessions.AnyAsync())
        {
            var currentYear = DateTime.Now.Year;
            var sessions = new List<Session>
            {
                new Session
                {
                    Name = $"Spring {currentYear}",
                    Code = $"SP{currentYear.ToString().Substring(2)}",
                    StartDate = new DateTime(currentYear, 3, 1),
                    EndDate = new DateTime(currentYear, 8, 31),
                    IsCurrentSession = false,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Session
                {
                    Name = $"Fall {currentYear}",
                    Code = $"FL{currentYear.ToString().Substring(2)}",
                    StartDate = new DateTime(currentYear, 9, 1),
                    EndDate = new DateTime(currentYear + 1, 2, 28),
                    IsCurrentSession = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Session
                {
                    Name = $"Spring {currentYear + 1}",
                    Code = $"SP{(currentYear + 1).ToString().Substring(2)}",
                    StartDate = new DateTime(currentYear + 1, 3, 1),
                    EndDate = new DateTime(currentYear + 1, 8, 31),
                    IsCurrentSession = false,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            context.Sessions.AddRange(sessions);
        }
    }

    private static async Task SeedRoomsAsync(ApplicationDbContext context)
    {
        if (!await context.Rooms.AnyAsync())
        {
            var rooms = new List<Room>
            {
                new Room
                {
                    RoomNumber = "LAB-01",
                    RoomName = "Computer Lab 1",
                    Capacity = 30,
                    RoomType = "Laboratory",
                    Building = "Main Building",
                    Floor = "Ground Floor",
                    Equipment = "30 Computers, Projector, Whiteboard",
                    HasProjector = true,
                    HasComputers = true,
                    HasAirConditioning = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Room
                {
                    RoomNumber = "LAB-02",
                    RoomName = "Electronics Lab",
                    Capacity = 25,
                    RoomType = "Laboratory",
                    Building = "Main Building",
                    Floor = "First Floor",
                    Equipment = "Workbenches, Oscilloscopes, Multimeters",
                    HasProjector = true,
                    HasComputers = false,
                    HasAirConditioning = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Room
                {
                    RoomNumber = "WS-01",
                    RoomName = "Welding Workshop",
                    Capacity = 15,
                    RoomType = "Workshop",
                    Building = "Workshop Building",
                    Floor = "Ground Floor",
                    Equipment = "Welding Stations, Safety Equipment",
                    HasProjector = false,
                    HasComputers = false,
                    HasAirConditioning = false,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Room
                {
                    RoomNumber = "CR-01",
                    RoomName = "Classroom 1",
                    Capacity = 40,
                    RoomType = "Classroom",
                    Building = "Main Building",
                    Floor = "Ground Floor",
                    Equipment = "Desks, Chairs, Whiteboard, Projector",
                    HasProjector = true,
                    HasComputers = false,
                    HasAirConditioning = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Room
                {
                    RoomNumber = "CR-02",
                    RoomName = "Classroom 2",
                    Capacity = 35,
                    RoomType = "Classroom",
                    Building = "Main Building",
                    Floor = "First Floor",
                    Equipment = "Desks, Chairs, Whiteboard",
                    HasProjector = false,
                    HasComputers = false,
                    HasAirConditioning = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            context.Rooms.AddRange(rooms);
        }
    }

    private static async Task SeedTimingsAsync(ApplicationDbContext context)
    {
        if (!await context.Timings.AnyAsync())
        {
            var timings = new List<Timing>
            {
                new Timing
                {
                    Name = "Morning Shift",
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    Shift = "Morning",
                    Description = "8:00 AM to 2:00 PM",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Timing
                {
                    Name = "Evening Shift",
                    StartTime = new TimeSpan(14, 30, 0),
                    EndTime = new TimeSpan(20, 30, 0),
                    Shift = "Evening",
                    Description = "2:30 PM to 8:30 PM",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Timing
                {
                    Name = "Weekend Morning",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(15, 0, 0),
                    Shift = "Weekend",
                    Description = "9:00 AM to 3:00 PM (Weekends)",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            context.Timings.AddRange(timings);
        }
    }
    
    private static async Task SeedTeachersAsync(ApplicationDbContext context)
    {
        if (!await context.Teachers.AnyAsync())
        {
            var teachers = new List<Teacher>
            {
                new Teacher
                {
                    TeacherCode = "TCH-001",
                    Username = "john.smith",
                    FirstName = "John",
                    LastName = "Smith",
                    FatherName = "Robert Smith",
                    CNIC = "12345-6789012-4",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Gender = "Male",
                    PhoneNumber = "+92 300 1234568",
                    Email = "john.smith@sms.com",
                    Address = "456 Teacher Street, Lahore",
                    City = "Lahore",
                    Province = "Punjab",
                    Country = "Pakistan",
                    PostalCode = "54000",
                    HireDate = DateTime.UtcNow.AddYears(-2),
                    Status = "Active",
                    Qualification = "MS Computer Science",
                    Specialization = "Programming & Software Development",
                    Salary = 45000,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Teacher
                {
                    TeacherCode = "TCH-002",
                    Username = "sarah.johnson",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    FatherName = "Michael Johnson",
                    CNIC = "12345-6789012-5",
                    DateOfBirth = new DateTime(1988, 8, 22),
                    Gender = "Female",
                    PhoneNumber = "+92 300 1234569",
                    Email = "sarah.johnson@sms.com",
                    Address = "789 Faculty Lane, Lahore",
                    City = "Lahore",
                    Province = "Punjab",
                    Country = "Pakistan",
                    PostalCode = "54000",
                    HireDate = DateTime.UtcNow.AddYears(-1),
                    Status = "Active",
                    Qualification = "BS Electrical Engineering",
                    Specialization = "Electronics & Circuit Design",
                    Salary = 42000,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Teacher
                {
                    TeacherCode = "TCH-003",
                    Username = "ahmed.hassan",
                    FirstName = "Ahmed",
                    LastName = "Hassan",
                    FatherName = "Muhammad Hassan",
                    CNIC = "12345-6789012-6",
                    DateOfBirth = new DateTime(1982, 12, 10),
                    Gender = "Male",
                    PhoneNumber = "+92 300 1234570",
                    Email = "ahmed.hassan@sms.com",
                    Address = "321 Instructor Road, Lahore",
                    City = "Lahore",
                    Province = "Punjab",
                    Country = "Pakistan",
                    PostalCode = "54000",
                    HireDate = DateTime.UtcNow.AddYears(-3),
                    Status = "Active",
                    Qualification = "Diploma in Mechanical Engineering",
                    Specialization = "Automotive Technology",
                    Salary = 40000,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Teacher
                {
                    TeacherCode = "TCH-004",
                    Username = "fatima.ali",
                    FirstName = "Fatima",
                    LastName = "Ali",
                    FatherName = "Ali Ahmad",
                    CNIC = "12345-6789012-7",
                    DateOfBirth = new DateTime(1990, 3, 18),
                    Gender = "Female",
                    PhoneNumber = "+92 300 1234571",
                    Email = "fatima.ali@sms.com",
                    Address = "654 Education Street, Lahore",
                    City = "Lahore",
                    Province = "Punjab",
                    Country = "Pakistan",
                    PostalCode = "54000",
                    HireDate = DateTime.UtcNow.AddMonths(-8),
                    Status = "Active",
                    Qualification = "BS Mathematics",
                    Specialization = "Mathematics & Physics",
                    Salary = 38000,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Teacher
                {
                    TeacherCode = "TCH-005",
                    Username = "muhammad.khan",
                    FirstName = "Muhammad",
                    LastName = "Khan",
                    FatherName = "Abdul Khan",
                    CNIC = "12345-6789012-8",
                    DateOfBirth = new DateTime(1986, 11, 5),
                    Gender = "Male",
                    PhoneNumber = "+92 300 1234572",
                    Email = "muhammad.khan@sms.com",
                    Address = "987 Training Avenue, Lahore",
                    City = "Lahore",
                    Province = "Punjab",
                    Country = "Pakistan",
                    PostalCode = "54000",
                    HireDate = DateTime.UtcNow.AddYears(-1).AddMonths(-6),
                    Status = "Active",
                    Qualification = "Diploma in Welding Technology",
                    Specialization = "Welding & Metal Fabrication",
                    Salary = 35000,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };
            
            context.Teachers.AddRange(teachers);
        }
    }
    
    private static async Task SeedBatchesAsync(ApplicationDbContext context)
    {
        if (!await context.Batches.AnyAsync())
        {
            // Get required data for batch creation
            var citTrade = await context.Trades.FirstOrDefaultAsync(t => t.Code == "CIT");
            var currentSession = await context.Sessions.FirstOrDefaultAsync(s => s.IsCurrentSession);
            var morningTiming = await context.Timings.FirstOrDefaultAsync(t => t.Name == "Morning Shift");
            var lab1Room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == "LAB-01");
            var primaryTeacher = await context.Teachers.FirstOrDefaultAsync(t => t.TeacherCode == "TCH-001");
            
            if (citTrade != null && currentSession != null)
            {
                var batch = new Batch
                {
                    BatchCode = "CIT-F24-01",
                    BatchName = "Computer Information Technology - Fall 2024 Batch 1",
                    TradeId = citTrade.Id,
                    SessionId = currentSession.Id,
                    TimingId = morningTiming?.Id,
                    RoomId = lab1Room?.Id,
                    PrimaryInstructorId = primaryTeacher?.Id,
                    StartDate = DateTime.UtcNow.AddDays(-15),
                    EndDate = DateTime.UtcNow.AddMonths(12),
                    MaxStudents = 30,
                    Status = "Active",
                    Description = "First batch for Computer Information Technology in Fall 2024 session",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                
                context.Batches.Add(batch);
                await context.SaveChangesAsync();
                
                // Assign the existing student to this batch
                var student = await context.Students.FirstOrDefaultAsync(s => s.RegistrationNumber == "CIT-2025-001");
                if (student != null)
                {
                    student.BatchId = batch.Id;
                    student.ModifiedDate = DateTime.UtcNow;
                    student.ModifiedBy = "System";
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
