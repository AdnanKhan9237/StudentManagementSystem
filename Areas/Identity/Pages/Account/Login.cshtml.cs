using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.Data;
using StudentManagementSystem.Constants;

namespace StudentManagementSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email / Username / CNIC / Phone is required.")]
            [Display(Name = "Email / Username / CNIC / Phone")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }

                returnUrl ??= Url.Content("~/Dashboard");

                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

                ReturnUrl = returnUrl;
            }
            catch (Exception)
            {
                // Log the error and set a simple return URL
                ReturnUrl = "/Dashboard";
                ExternalLogins = new List<AuthenticationScheme>();
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                ApplicationUser? user = null;
                
                // Check if input is an email format
                if (Input.Email.Contains('@'))
                {
                    // Try to find by email first
                    user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);
                }
                
                // If not found by email (or input wasn't email), try username
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(Input.Email);
                }

                // Try by Student CNIC
                if (user == null)
                {
                    var studentByCnic = await _db.Students
                        .AsNoTracking()
                        .Where(s => s.CNIC == Input.Email && s.UserId != null)
                        .Select(s => s.UserId)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(studentByCnic))
                    {
                        user = await _userManager.FindByIdAsync(studentByCnic!);
                    }
                }

                // Try by Student PhoneNumber
                if (user == null)
                {
                    var studentByPhone = await _db.Students
                        .AsNoTracking()
                        .Where(s => s.PhoneNumber == Input.Email && s.UserId != null)
                        .Select(s => s.UserId)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(studentByPhone))
                    {
                        user = await _userManager.FindByIdAsync(studentByPhone!);
                    }
                }

                // Try by User PhoneNumber directly
                if (user == null)
                {
                    user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == Input.Email);
                }

                // Auto-provision student using CNIC/Phone with default password
                if (user == null)
                {
                    var candidate = await _db.Students.FirstOrDefaultAsync(s => !s.IsDeleted && (s.CNIC == Input.Email || s.PhoneNumber == Input.Email));
                    if (candidate != null && string.IsNullOrEmpty(candidate.UserId))
                    {
                        if (Input.Password == "sostti123+")
                        {
                            var userName = !string.IsNullOrWhiteSpace(candidate.CNIC) ? candidate.CNIC! : (candidate.PhoneNumber ?? candidate.Email ?? Guid.NewGuid().ToString("N"));
                            var newUser = new ApplicationUser
                            {
                                UserName = userName,
                                Email = candidate.Email ?? $"{userName}@noemail.local",
                                FirstName = candidate.FirstName,
                                LastName = candidate.LastName,
                                PhoneNumber = candidate.PhoneNumber,
                                EmailConfirmed = true,
                                IsActive = true,
                                MustChangePassword = true,
                                CreatedDate = DateTime.UtcNow,
                                CreatedBy = "System"
                            };

                            var createResult = await _userManager.CreateAsync(newUser, Input.Password);
                            if (createResult.Succeeded)
                            {
                                if (!await _roleManager.RoleExistsAsync(UserRoles.Student))
                                {
                                    await _roleManager.CreateAsync(new IdentityRole(UserRoles.Student));
                                }
                                await _userManager.AddToRoleAsync(newUser, UserRoles.Student);

                                candidate.UserId = newUser.Id;
                                await _db.SaveChangesAsync();
                                user = newUser;
                            }
                            else
                            {
                                foreach (var error in createResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return Page();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Account not found. Use default password to activate your account.");
                            return Page();
                        }
                    }
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
                    return Page();
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Force password change if required
                    if (user.MustChangePassword)
                    {
                        TempData["InfoMessage"] = "Please change your password before continuing.";
                        return RedirectToPage("/Account/Manage/ChangePassword", new { area = "Identity" });
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}