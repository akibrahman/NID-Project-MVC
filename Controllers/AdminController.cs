using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NID_Project.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var moderators = await _userManager.GetUsersInRoleAsync("Moderator");
            return View(moderators);
        }

        [HttpGet]
        public IActionResult CreateModerator()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModerator(CreateModeratorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Moderator");
                    TempData["SuccessMessage"] = "Moderator created successfully.";
                    return RedirectToAction("Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockModerator(string id)
        {
            var mod = await _userManager.FindByIdAsync(id);
            if (mod != null && await _userManager.IsInRoleAsync(mod, "Moderator"))
            {
                mod.IsBlocked = true;
                await _userManager.UpdateAsync(mod);
                TempData["SuccessMessage"] = "Moderator blocked.";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockModerator(string id)
        {
            var mod = await _userManager.FindByIdAsync(id);
            if (mod != null && await _userManager.IsInRoleAsync(mod, "Moderator"))
            {
                mod.IsBlocked = false;
                await _userManager.UpdateAsync(mod);
                TempData["SuccessMessage"] = "Moderator unblocked.";
            }
            return RedirectToAction("Dashboard");
        }
    }
}