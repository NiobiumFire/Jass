using BelotWebApp.Data;
using BelotWebApp.Models.Administration;
using BelotWebApp.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ServerNotificationManager _notificationManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, ServerNotificationManager notificationManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _notificationManager = notificationManager;

        }

        [HttpGet]
        [Route("Admin/UserAdmin")]
        public async Task<IActionResult> Index()
        {
            return View("UserAdmin", await GetUserAdminModel());
        }

        private async Task<AdminUserModel> GetUserAdminModel()
        {
            var model = new AdminUserModel();
            foreach (ApplicationUser user in _userManager.Users)
            {
                var administrateUserModel = new InputModel()
                {
                    Username = user.UserName,
                    Email = user.Email,
                };
                foreach (IdentityRole role in _roleManager.Roles)
                {
                    administrateUserModel.IsInRole.Add(await _userManager.IsInRoleAsync(user, role.Name));
                }
                model.Users.Add(administrateUserModel);
            }
            model.Users = model.Users.OrderBy(u => u.Username).ToList();
            return model;
        }

        [HttpPost]
        public async Task<IActionResult> Index(AdminUserModel model)
        {
            foreach (InputModel updatedUser in model.Users)
            {
                var user = await _userManager.FindByNameAsync(updatedUser.Username);
                if (user == null)
                {
                    ModelState.AddModelError("", @"User '" + updatedUser.Username + @"' could not be found.");
                    continue;
                }

                var roles = _roleManager.Roles.ToArray();
                for (int i = 0; i < roles.Length; i++)
                {
                    IdentityResult? result = null;
                    if (updatedUser.IsInRole[i] && !await _userManager.IsInRoleAsync(user, roles[i].Name))
                    {
                        result = await _userManager.AddToRoleAsync(user, roles[i].Name);
                    }
                    else if (!updatedUser.IsInRole[i] && await _userManager.IsInRoleAsync(user, roles[i].Name))
                    {
                        result = await _userManager.RemoveFromRoleAsync(user, roles[i].Name);
                    }
                    if (result != null && !result.Succeeded)
                    {
                        ModelState.AddModelError("", @"Failed to update roles for user '" + updatedUser.Username + @"'.");
                    }
                }
            }
            await _signInManager.RefreshSignInAsync(await _userManager.GetUserAsync(User));
            return View("UserAdmin", await GetUserAdminModel());
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", @"Failed to delete user '" + user.UserName + @"'.");
                }
                else if (user.Id == _userManager.GetUserId(User))
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ModelState.AddModelError("", @"User with username='" + username + @"' could not be found.");
            }
            return View("UserAdmin", await GetUserAdminModel());
        }

        public IActionResult Notification()
        {
            var model = _notificationManager.Current.Clone();
            if (model.ScheduledUtc == default)
            {
                model.ScheduledUtc = DateTime.UtcNow;
            }
            return View("Notification", model);
        }

        [HttpPost]
        public IActionResult Notification(ServerNotification model)
        {
            if (!ModelState.IsValid)
            {
                return View("Notification", model);
            }

            model.ScheduledUtc = DateTime.SpecifyKind(model.ScheduledUtc, DateTimeKind.Utc);

            _notificationManager.Update(model);
            return RedirectToAction(nameof(Notification));
        }
    }
}
