using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Constants;
using StudentManagementSystem.Models.Entities;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Admin}")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index(int page = 1, string searchTerm = "", string roleFilter = "")
        {
            const int pageSize = 20;

            var users = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(u => (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                                        (u.Email != null && u.Email.Contains(searchTerm)) ||
                                        (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                                        (u.LastName != null && u.LastName.Contains(searchTerm)));
            }

            var totalUsers = await users.CountAsync();
            var userList = await users
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<UserViewModel>();
            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd < DateTime.UtcNow,
                    Roles = roles.ToList(),
                    CreatedDate = user.CreatedDate
                });
            }

            // Filter by role if specified
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                userViewModels = userViewModels.Where(u => u.Roles.Contains(roleFilter)).ToList();
                totalUsers = userViewModels.Count;
            }

            var model = new UserListViewModel
            {
                Users = userViewModels,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                PageNumber = page,
                PageSize = pageSize,
                TotalRecords = totalUsers,
                AvailableRoles = await GetAvailableRolesAsync()
            };

            return View(model);
        }

        // GET: UserManagement/Create
        public IActionResult Create()
        {
            var model = new CreateUserViewModel();
            return View(model);
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email, // Use email as username
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    CNIC = model.CNIC,
                    Department = model.Department,
                    Designation = model.Designation,
                    EmailConfirmed = true,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign the selected role
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(model);
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd < DateTime.UtcNow,
                SelectedRoles = userRoles.ToList(),
                AvailableRoles = await GetAvailableRolesForCreationAsync()
            };

            return View(model);
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.ModifiedDate = DateTime.UtcNow;
                user.ModifiedBy = User.Identity?.Name ?? "System";

                // Handle account lockout
                if (!model.IsActive && (!user.LockoutEnd.HasValue || user.LockoutEnd < DateTime.UtcNow))
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }
                else if (model.IsActive && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles ?? new List<string>());
                    var rolesToAdd = (model.SelectedRoles ?? new List<string>()).Except(currentRoles);

                    if (rolesToRemove.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    }

                    if (rolesToAdd.Any())
                    {
                        await _userManager.AddToRolesAsync(user, rolesToAdd);
                    }

                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            model.AvailableRoles = await GetAvailableRolesForCreationAsync();
            return View(model);
        }

        // POST: UserManagement/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Password reset successfully!" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to reset password: {errors}" });
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                return Json(new { success = false, message = "You cannot delete your own account." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User {user.Email} deleted by {User.Identity?.Name}");
                return Json(new { success = true, message = "User deleted successfully!" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Failed to delete user: {errors}" });
        }

        private async Task<List<SelectListItem>> GetAvailableRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.Select(r => new SelectListItem
            {
                Text = r.Name ?? string.Empty,
                Value = r.Name ?? string.Empty
            }).ToList();
        }

        private async Task<List<SelectListItem>> GetAvailableRolesForCreationAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return new List<SelectListItem>();
            }
            
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var allRoles = await _roleManager.Roles.ToListAsync();

            // SuperAdmin can assign any role
            if (currentUserRoles.Contains(UserRoles.SuperAdmin))
            {
                return allRoles.Select(r => new SelectListItem
                {
                    Text = r.Name ?? string.Empty,
                    Value = r.Name ?? string.Empty
                }).ToList();
            }

            // Admin can assign all roles except SuperAdmin
            if (currentUserRoles.Contains(UserRoles.Admin))
            {
                return allRoles
                    .Where(r => r.Name != UserRoles.SuperAdmin)
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name ?? string.Empty,
                        Value = r.Name ?? string.Empty
                    }).ToList();
            }

            return new List<SelectListItem>();
        }
    }
}