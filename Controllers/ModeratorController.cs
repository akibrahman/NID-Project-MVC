using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NID_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

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

        public async Task<IActionResult> UserDetails(string id, string returnTo)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                // If user not found, redirect to appropriate list
                if (returnTo == "allusers")
                    return RedirectToAction("AllUsers");
                else
                    return RedirectToAction("PendingUsers");
            }

            ViewBag.ReturnTo = returnTo;
            return View(user);
        }

        private async Task<string> GenerateUniqueNidNumber()
        {
            var random = new Random();
            const int maxAttempts = 20;
            for (int i = 0; i < maxAttempts; i++)
            {
                // Generate random 10-digit number (first digit not zero)
                string candidate = random.Next(1000000000, 1000000000 + 900000000).ToString(); // 1,000,000,000 to 9,999,999,999? Actually 1000000000 to 1999999999? Need fix.
                                                                                               // Better: generate from 1000000000 to 9999999999
                long number = random.NextInt64(1000000000L, 10000000000L); // 10 digits, lower bound inclusive, upper exclusive
                candidate = number.ToString();

                // Check uniqueness
                bool exists = await _userManager.Users.AnyAsync(u => u.NIDNumber == candidate);
                if (!exists)
                    return candidate;
            }
            return null; // or throw exception; for now return null to indicate failure
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var user = await _userManager.FindByIdAsync(id);
            if (user != null && !user.IsApproved)
            {
                // Generate unique 10-digit NID number
                string nidNumber = await GenerateUniqueNidNumber();
                if (nidNumber == null)
                {
                    TempData["ErrorMessage"] = "Could not generate a unique NID number. Please try again.";
                    return RedirectToAction("PendingUsers");
                }

                user.NIDNumber = nidNumber;
                user.IsApproved = true;
                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User approved successfully. NID Number: {nidNumber}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update user.";
                }
            }
            return RedirectToAction("PendingUsers");
        }

        public async Task<IActionResult> AllUsers(int? pageNumber)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            int pageSize = 10;
            var users = await _userManager.GetUsersInRoleAsync("User");
            var approvedUsers = users.Where(u => u.IsApproved).OrderBy(u => u.FullName).ToList();

            return View(PaginatedList<ApplicationUser>.Create(approvedUsers, pageNumber ?? 1, pageSize));
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