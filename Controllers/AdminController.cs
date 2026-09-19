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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext context,
           IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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

        // ============ VOTING MANAGEMENT ============

        public async Task<IActionResult> Votings()
        {
            var list = await _context.Votings
                .Include(v => v.Nominees)
                .Include(v => v.Ballots)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateVoting()
        {
            return View(new CreateVotingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoting(CreateVotingViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var voting = new Voting
            {
                Title = model.Title,
                Area = model.Area,
                Position = model.Position,
                Description = model.Description,
                Status = VotingStatus.Opening,
                CreatedAt = DateTime.UtcNow
            };
            _context.Votings.Add(voting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Voting created. Add nominees next.";
            return RedirectToAction("VotingDetails", new { id = voting.Id });
        }

        public async Task<IActionResult> VotingDetails(int id)
        {
            var voting = await _context.Votings
                .Include(v => v.Nominees)
                .Include(v => v.Ballots)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (voting == null) return NotFound();

            // Statistics
            var users = await _userManager.GetUsersInRoleAsync("User");
            var eligibleUsers = users.Where(u => u.IsApproved && !u.IsBlocked).ToList();

            ViewBag.TotalEligible = eligibleUsers.Count;
            ViewBag.TotalVoted = voting.Ballots.Count;
            ViewBag.Remaining = eligibleUsers.Count - voting.Ballots.Count;

            return View(voting);
        }

        [HttpGet]
        public async Task<IActionResult> AddNominee(int id)
        {
            var voting = await _context.Votings
                .Include(v => v.Nominees)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (voting == null) return NotFound();
            if (voting.Status != VotingStatus.Opening)
            {
                TempData["ErrorMessage"] = "Nominees can only be added before the voting starts.";
                return RedirectToAction("VotingDetails", new { id });
            }
            if (voting.Nominees.Count >= 10)
            {
                TempData["ErrorMessage"] = "Maximum 10 nominees allowed.";
                return RedirectToAction("VotingDetails", new { id });
            }

            return View(new AddNomineeViewModel { VotingId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNominee(AddNomineeViewModel model)
        {
            var voting = await _context.Votings
                .Include(v => v.Nominees)
                .FirstOrDefaultAsync(v => v.Id == model.VotingId);
            if (voting == null) return NotFound();

            if (voting.Status != VotingStatus.Opening)
            {
                TempData["ErrorMessage"] = "Nominees can only be added before the voting starts.";
                return RedirectToAction("VotingDetails", new { id = voting.Id });
            }
            if (voting.Nominees.Count >= 10)
            {
                TempData["ErrorMessage"] = "Maximum 10 nominees allowed.";
                return RedirectToAction("VotingDetails", new { id = voting.Id });
            }
            if (!ModelState.IsValid) return View(model);

            string? photoPath = null;
            if (model.Photo != null)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ModelState.AddModelError("Photo", "Only JPG, JPEG, or PNG files are allowed.");
                    return View(model);
                }
                var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(model.Photo.FileName);
                var filePath = Path.Combine(folder, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create))
                    await model.Photo.CopyToAsync(fs);
                photoPath = "/uploads/" + fileName;
            }

            _context.Nominees.Add(new Nominee
            {
                VotingId = voting.Id,
                Name = model.Name,
                TeamName = model.TeamName,
                Sign = model.Sign,
                PhotoPath = photoPath
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Nominee added.";
            return RedirectToAction("VotingDetails", new { id = voting.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartVoting(int id)
        {
            var voting = await _context.Votings
                .Include(v => v.Nominees)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (voting == null) return NotFound();

            if (voting.Status != VotingStatus.Opening)
            {
                TempData["ErrorMessage"] = "Voting can only be started from Opening status.";
                return RedirectToAction("VotingDetails", new { id });
            }
            if (voting.Nominees.Count < 3)
            {
                TempData["ErrorMessage"] = "At least 3 nominees are required to start the voting.";
                return RedirectToAction("VotingDetails", new { id });
            }

            voting.Status = VotingStatus.Running;
            voting.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Voting is now running. Users can vote.";
            return RedirectToAction("VotingDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseVoting(int id)
        {
            var voting = await _context.Votings.FirstOrDefaultAsync(v => v.Id == id);
            if (voting == null) return NotFound();

            if (voting.Status != VotingStatus.Running)
            {
                TempData["ErrorMessage"] = "Only running votings can be closed.";
                return RedirectToAction("VotingDetails", new { id });
            }

            voting.Status = VotingStatus.Closed;
            voting.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Voting closed. Results are now visible to everyone.";
            return RedirectToAction("VotingDetails", new { id });
        }
    }
}