using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NID_Project.Data;
using NID_Project.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "User")]
    public class VotingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VotingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Votings
                .Include(v => v.Nominees)
                .Include(v => v.Ballots)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var voting = await _context.Votings
                .Include(v => v.Nominees)
                    .ThenInclude(n => n.Ballots)
                .Include(v => v.Ballots)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (voting == null) return NotFound();

            var myBallot = voting.Ballots.FirstOrDefault(b => b.UserId == user.Id);
            ViewBag.MyBallot = myBallot;
            ViewBag.IsBlocked = user.IsBlocked;
            ViewBag.IsApproved = user.IsApproved;

            return View(voting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(int votingId, int nomineeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (user.IsBlocked || !user.IsApproved)
            {
                TempData["ErrorMessage"] = "You are not eligible to vote.";
                return RedirectToAction("Details", new { id = votingId });
            }

            var voting = await _context.Votings
                .Include(v => v.Nominees)
                .FirstOrDefaultAsync(v => v.Id == votingId);
            if (voting == null) return NotFound();
            if (voting.Status != VotingStatus.Running)
            {
                TempData["ErrorMessage"] = "This voting is not running.";
                return RedirectToAction("Details", new { id = votingId });
            }

            if (!voting.Nominees.Any(n => n.Id == nomineeId))
            {
                TempData["ErrorMessage"] = "Invalid nominee.";
                return RedirectToAction("Details", new { id = votingId });
            }

            bool alreadyVoted = await _context.UserBallots
                .AnyAsync(b => b.VotingId == votingId && b.UserId == user.Id);
            if (alreadyVoted)
            {
                TempData["ErrorMessage"] = "You have already voted in this voting.";
                return RedirectToAction("Details", new { id = votingId });
            }

            _context.UserBallots.Add(new UserBallot
            {
                VotingId = votingId,
                NomineeId = nomineeId,
                UserId = user.Id,
                VotedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your vote has been submitted.";
            return RedirectToAction("Details", new { id = votingId });
        }
    }
}