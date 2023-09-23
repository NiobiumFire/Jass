using BelotWebApp.Areas.Identity.Data;
using BelotWebApp.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdministrationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdministrationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await GetModel());
        }

        private async Task<AdministrateUserModel> GetModel()
        {
            var model = new AdministrateUserModel();
            foreach (ApplicationUser user in _userManager.Users)
            {
                var administrateUserModel = new BelotWebApp.Models.Administration.InputModel()
                {
                    Id = user.Id,
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
        public async Task<IActionResult> Index(AdministrateUserModel model)
        {
            List<string> errors = new List<string>();
            foreach (BelotWebApp.Models.Administration.InputModel updatedUser in model.Users)
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
                    IdentityResult result = null;
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
            return View(await GetModel());
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", @"Failed to delete user '" + user.UserName + @"'.");
                }
                else if (id == _userManager.GetUserId(User))
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ModelState.AddModelError("", @"User with id='" + id + @"' could not be found.");
            }
            return View("Index", await GetModel());
        }
    }
}
