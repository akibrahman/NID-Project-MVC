using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NID_Project.Models;
using System.Linq;
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

        // Helper to check if current moderator is blocked
        private async Task<bool> IsBlocked()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.IsBlocked ?? true;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            return View(user);
        }

        public async Task<IActionResult> PendingUsers()
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var users = await _userManager.GetUsersInRoleAsync("User");
            var pendingUsers = users.Where(u => !u.IsApproved).ToList();
            return View(pendingUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && !user.IsApproved)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "User approved successfully.";
            }
            return RedirectToAction("PendingUsers");
        }

        public async Task<IActionResult> AllUsers()
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var users = await _userManager.GetUsersInRoleAsync("User");
            var approvedUsers = users.Where(u => u.IsApproved).ToList();
            return View(approvedUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string id)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && await _userManager.IsInRoleAsync(user, "User"))
            {
                user.IsBlocked = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "User blocked successfully.";
            }
            return RedirectToAction("AllUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string id)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && await _userManager.IsInRoleAsync(user, "User"))
            {
                user.IsBlocked = false;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "User unblocked successfully.";
            }
            return RedirectToAction("AllUsers");
        }

        public IActionResult Blocked()
        {
            return View();
        }
    }
}