using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NID_Project.Data;
using NID_Project.Models;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var moderators = await _userManager.GetUsersInRoleAsync("Moderator");
            var users = await _userManager.GetUsersInRoleAsync("User");

            // ---- Edit applications ----
            var applications = await _context.EditApplications
                .Include(a => a.ReviewedByModerator)
                .Include(a => a.ChangeReviewedByModerator)
                .ToListAsync();

            var applicationsByStatus = applications
                .GroupBy(a => a.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // ---- Moderator activity: count of stage 1 + stage 2 reviews per moderator ----
            var activity = new Dictionary<string, int>();

            // Initialize with all moderators (so those with 0 reviews still appear)
            foreach (var mod in moderators)
            {
                activity[mod.FullName ?? mod.Email ?? "Unknown"] = 0;
            }

            foreach (var app in applications)
            {
                if (app.ReviewedByModerator != null)
                {
                    var name = app.ReviewedByModerator.FullName ?? app.ReviewedByModerator.Email ?? "Unknown";
                    if (!activity.ContainsKey(name)) activity[name] = 0;
                    activity[name]++;
                }
                if (app.ChangeReviewedByModerator != null)
                {
                    var name = app.ChangeReviewedByModerator.FullName ?? app.ChangeReviewedByModerator.Email ?? "Unknown";
                    if (!activity.ContainsKey(name)) activity[name] = 0;
                    activity[name]++;
                }
            }

            // Sort descending by activity (optional but nice)
            activity = activity.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

            var model = new AdminDashboardViewModel
            {
                TotalModerators = moderators.Count,
                TotalUsers = users.Count,
                TotalPendingUsers = users.Count(u => !u.IsApproved),
                TotalApplications = applications.Count,
                ApplicationsByStatus = applicationsByStatus,
                ModeratorActivity = activity
            };

            return View(model);
        }

        public async Task<IActionResult> Moderators()
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
                    return RedirectToAction("Moderators");
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
            return RedirectToAction("Moderators");
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
            return RedirectToAction("Moderators");
        }
    }
}