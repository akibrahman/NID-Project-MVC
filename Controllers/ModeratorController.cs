using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NID_Project.Models;
using System.Threading.Tasks;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "Moderator")]
    public class ModeratorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ModeratorController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }
    }
}