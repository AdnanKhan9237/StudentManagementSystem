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

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed users
        await SeedUsersAsync(userManager, context);

        // Seed trades
        await SeedTradesAsync(context);

        // Seed sessions
        await SeedSessionsAsync(context);

        // Seed rooms
        await SeedRoomsAsync(context);

        // Seed timings
        await SeedTimingsAsync(context);

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
        // SuperAdmin
        await CreateUserIfNotExistsAsync(userManager, "superadmin@sms.com", "SuperAdmin@123", 
            "Super", "Admin", UserRoles.SuperAdmin, "IT", "System Administrator");
        
        // Admin
        await CreateUserIfNotExistsAsync(userManager, "admin@sms.com", "Admin@123", 
            "Admin", "User", UserRoles.Admin, "Administration", "Administrator");
        
        // Teacher
        await CreateUserIfNotExistsAsync(userManager, "teacher@sms.com", "Teacher@123", 
            "John", "Teacher", UserRoles.Teacher, "Academic", "Instructor");
        
        // Accounts
        await CreateUserIfNotExistsAsync(userManager, "accounts@sms.com", "Accounts@123", 
            "Finance", "Manager", UserRoles.Accounts, "Finance", "Accounts Manager");
        
        // Student User - will be linked to a student record
        var studentUser = await CreateUserIfNotExistsAsync(userManager, "student@sms.com", "Student@123", 
            "Ahmad", "Ali", UserRoles.Student, null, null);
        
        // Create sample student record linked to student user
        if (studentUser != null)
        {
            await CreateSampleStudentAsync(context, studentUser.Id);
        }
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
    
    private static async Task CreateSampleStudentAsync(ApplicationDbContext context, string userId)
    {
        // Check if student already exists
        if (await context.Students.AnyAsync(s => s.UserId == userId))
            return;
            
        // Get the first trade and session for the sample student
        var trade = await context.Trades.FirstOrDefaultAsync(t => t.Code == "CIT");
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.IsCurrentSession);
        
        if (trade != null && session != null)
        {
            var student = new Student
            {
                UserId = userId,
                RegistrationNumber = "CIT-2025-001",
                StudentCode = "STD-001",
                FirstName = "Ahmad",
                LastName = "Ali",
                FatherName = "Muhammad Ali",
                Email = "student@sms.com",
                PhoneNumber = "+92 300 1234567",
                CNIC = "12345-6789012-3",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Male",
                Address = "123 Main Street, Lahore",
                City = "Lahore",
                Province = "Punjab",
                Country = "Pakistan",
                PostalCode = "54000",
                TradeId = trade.Id,
                SessionId = session.Id,
                AdmissionDate = DateTime.UtcNow.AddDays(-30),
                TotalFee = trade.TotalFee,
                PaidAmount = 10000, // Partial payment
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            };
            
            context.Students.Add(student);
            await context.SaveChangesAsync();
        }
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
}