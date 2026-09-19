using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NID_Project.Data;
using NID_Project.Models;
using System;
using System.Linq;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "Moderator")]
    public class ModeratorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ModeratorController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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

        public async Task<IActionResult> Applications()
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var apps = await _context.EditApplications
                .Include(a => a.User)
                .Include(a => a.ReviewedByModerator)
                .Include(a => a.ChangeReviewedByModerator)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(apps);
        }

        public async Task<IActionResult> ApplicationDetails(int id)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var app = await _context.EditApplications
                .Include(a => a.User)
                .Include(a => a.ReviewedByModerator)
                .Include(a => a.ChangeReviewedByModerator)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();

            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewApplication(int id, string action, string? message)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var mod = await _userManager.GetUserAsync(User);
            var app = await _context.EditApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] = "This application is not pending.";
                return RedirectToAction("ApplicationDetails", new { id });
            }

            if (action == "reject" && string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Rejection message is required.";
                return RedirectToAction("ApplicationDetails", new { id });
            }

            app.ReviewedByModeratorId = mod!.Id;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewMessage = message;
            app.Status = action == "approve" ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Application {(action == "approve" ? "approved" : "rejected")}.";
            return RedirectToAction("ApplicationDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewChanges(int id, string action, string? message)
        {
            if (await IsBlocked()) return RedirectToAction("Blocked");

            var mod = await _userManager.GetUserAsync(User);
            var app = await _context.EditApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.Status != ApplicationStatus.Edited)
            {
                TempData["ErrorMessage"] = "No edits are awaiting review.";
                return RedirectToAction("ApplicationDetails", new { id });
            }

            if (action == "reject" && string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Rejection message is required.";
                return RedirectToAction("ApplicationDetails", new { id });
            }

            app.ChangeReviewedByModeratorId = mod!.Id;
            app.ChangeReviewedAt = DateTime.UtcNow;
            app.ChangeReviewMessage = message;

            if (action == "approve")
            {
                var user = await _userManager.FindByIdAsync(app.UserId);
                if (user == null) return NotFound();

                var json = app.EditedDataJson ?? "{}";
                Dictionary<string, FieldChange> changes;
                try
                {
                    changes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, FieldChange>>(json)
                              ?? new Dictionary<string, FieldChange>();
                }
                catch
                {
                    // Fallback for applications created before the FieldChange migration
                    var oldStyle = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string?>>(json)
                        ?? new Dictionary<string, string?>();
                    changes = oldStyle.ToDictionary(kv => kv.Key, kv => new FieldChange { OldValue = null, NewValue = kv.Value });
                }

                foreach (var kv in changes)
                {
                    var newValue = kv.Value.NewValue;
                    switch (kv.Key)
                    {
                        case "FullName": user.FullName = newValue ?? user.FullName; break;
                        case "FatherName": user.FatherName = newValue; break;
                        case "MotherName": user.MotherName = newValue; break;
                        case "DateOfBirth": if (DateTime.TryParse(newValue, out var d)) user.DateOfBirth = d; break;
                        case "Gender": user.Gender = newValue; break;
                        case "Nationality": user.Nationality = newValue; break;
                        case "Religion": user.Religion = newValue; break;
                        case "Occupation": user.Occupation = newValue; break;
                        case "BloodGroup": user.BloodGroup = newValue; break;
                        case "PresentAddress": user.PresentAddress = newValue; break;
                        case "PermanentAddress": user.PermanentAddress = newValue; break;
                        case "PhotoPath": user.PhotoPath = newValue; break;
                    }
                }
                await _userManager.UpdateAsync(user);
                app.Status = ApplicationStatus.Completed;
                TempData["SuccessMessage"] = "Changes approved and applied to the user.";
            }
            else
            {
                app.Status = ApplicationStatus.ChangeRejected;
                TempData["SuccessMessage"] = "Changes rejected.";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("ApplicationDetails", new { id });
        }

        public IActionResult Blocked()
        {
            return View();
        }
    }
}