using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NID_Project.Data;
using NID_Project.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace NID_Project.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadNidPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Check if user is approved and not blocked
            if (!user.IsApproved || user.IsBlocked)
            {
                TempData["ErrorMessage"] = "You cannot download your NID because your account is not approved or is blocked.";
                return RedirectToAction("Dashboard");
            }

            // Generate PDF
            var pdfBytes = GenerateNidPdf(user);
            return File(pdfBytes, "application/pdf", $"NID_{user.NIDNumber ?? user.FullName}.pdf");
        }

        private byte[] GenerateNidPdf(ApplicationUser user)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Load photo bytes if available
            byte[]? photoBytes = null;
            if (!string.IsNullOrEmpty(user.PhotoPath))
            {
                var photoPath = Path.Combine(_webHostEnvironment.WebRootPath, user.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(photoPath))
                {
                    try
                    {
                        using var bitmap = SkiaSharp.SKBitmap.Decode(photoPath);
                        if (bitmap != null)
                        {
                            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
                            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            photoBytes = data.ToArray();
                        }
                    }
                    catch { }
                }
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Standard credit card size: 85.6mm x 54mm = 242.65 x 153.07 points
                    page.Size(new PageSize(242.65f, 153.07f, Unit.Point));
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(7));

                    // White background
                    page.PageColor(Colors.White);

                    // Main container with border
                    page.Content()
                        .Padding(10)
                        .Column(col =>
                        {
                            // Header
                            col.Item()
                                .Background(Colors.Blue.Darken3)
                                .PaddingVertical(4)
                                .AlignCenter()
                                .Text("National Identity Card")
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(Colors.White);

                            // Body: one row with two columns, both vertically centered
                            col.Item()
                                .PaddingTop(5)
                                .Row(row =>
                                {
                                    // Left column: Photo
                                    row.ConstantItem(80).Column(photoCol =>
                                    {
                                        photoCol.Item()
                                            .Width(70)
                                            .Height(90)
                                            .Background(Colors.White)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Column(inner =>
                                            {
                                                if (photoBytes != null)
                                                {
                                                    inner.Item().Image(photoBytes).FitArea();
                                                }
                                                else
                                                {
                                                    inner.Item().Text("No Photo").FontSize(8);
                                                }
                                            });
                                    });

                                    // Right column: Details
                                    row.RelativeItem().PaddingLeft(10).Column(infoCol =>
                                    {
                                        infoCol.Spacing(3);

                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("NID No:").SemiBold();
                                            r.RelativeItem(2).Text(user.NIDNumber ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Name:").SemiBold();
                                            r.RelativeItem(2).Text(user.FullName);
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Father:").SemiBold();
                                            r.RelativeItem(2).Text(user.FatherName ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Mother:").SemiBold();
                                            r.RelativeItem(2).Text(user.MotherName ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("DOB:").SemiBold();
                                            r.RelativeItem(2).Text(user.DateOfBirth?.ToString("dd MMM yyyy") ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Gender:").SemiBold();
                                            r.RelativeItem(2).Text(user.Gender ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Blood:").SemiBold();
                                            r.RelativeItem(2).Text(user.BloodGroup ?? "N/A");
                                        });
                                        infoCol.Item().Row(r =>
                                        {
                                            r.RelativeItem(1).Text("Address:").SemiBold();
                                            r.RelativeItem(2).Text(user.PresentAddress ?? "N/A");
                                        });
                                    });
                                });
                        });
                });
            });

            return document.GeneratePdf();
        }

        // ----- My Applications list -----
        public async Task<IActionResult> MyApplication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var apps = await _context.EditApplications
                .Include(a => a.ReviewedByModerator)
                .Include(a => a.ChangeReviewedByModerator)
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var lastApp = apps.FirstOrDefault();
            var canCreate = lastApp == null || lastApp.CreatedAt.AddMonths(1) <= DateTime.UtcNow;
            ViewBag.CanCreate = canCreate && !user.IsBlocked;

            if (!canCreate && lastApp != null)
                ViewBag.DaysUntilNext = (int)Math.Ceiling((lastApp.CreatedAt.AddMonths(1) - DateTime.UtcNow).TotalDays);

            var active = apps.FirstOrDefault(a =>
                a.Status == ApplicationStatus.Pending ||
                a.Status == ApplicationStatus.Approved ||
                a.Status == ApplicationStatus.Edited);
            ViewBag.ActiveApp = active;
            ViewBag.IsBlocked = user.IsBlocked;

            return View(apps);
        }

        // ----- Create application -----
        [HttpGet]
        public async Task<IActionResult> CreateApplication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.IsBlocked) { TempData["ErrorMessage"] = "Blocked users cannot apply."; return RedirectToAction("MyApplication"); }

            var lastApp = await _context.EditApplications
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync();
            if (lastApp != null && lastApp.CreatedAt.AddMonths(1) > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "You cannot apply yet.";
                return RedirectToAction("MyApplication");
            }
            if (await _context.EditApplications.AnyAsync(a => a.UserId == user.Id &&
                (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Edited)))
            {
                TempData["ErrorMessage"] = "You have an active application.";
                return RedirectToAction("MyApplication");
            }

            return View(new CreateApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApplication(CreateApplicationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.IsBlocked) { TempData["ErrorMessage"] = "Blocked users cannot apply."; return RedirectToAction("MyApplication"); }

            var lastApp = await _context.EditApplications
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync();
            if (lastApp != null && lastApp.CreatedAt.AddMonths(1) > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "You cannot apply yet.";
                return RedirectToAction("MyApplication");
            }
            if (await _context.EditApplications.AnyAsync(a => a.UserId == user.Id &&
                (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Edited)))
            {
                TempData["ErrorMessage"] = "You have an active application.";
                return RedirectToAction("MyApplication");
            }

            var fields = new List<string>();
            if (model.ChangeFullName) fields.Add("FullName");
            if (model.ChangeFatherName) fields.Add("FatherName");
            if (model.ChangeMotherName) fields.Add("MotherName");
            if (model.ChangeDateOfBirth) fields.Add("DateOfBirth");
            if (model.ChangeGender) fields.Add("Gender");
            if (model.ChangeNationality) fields.Add("Nationality");
            if (model.ChangeReligion) fields.Add("Religion");
            if (model.ChangeOccupation) fields.Add("Occupation");
            if (model.ChangeBloodGroup) fields.Add("BloodGroup");
            if (model.ChangePresentAddress) fields.Add("PresentAddress");
            if (model.ChangePermanentAddress) fields.Add("PermanentAddress");
            if (model.ChangePhoto) fields.Add("PhotoPath");

            if (!fields.Any())
            {
                ModelState.AddModelError("", "Select at least one field to change.");
                return View(model);
            }

            _context.EditApplications.Add(new EditApplication
            {
                UserId = user.Id,
                RequestedFields = string.Join(",", fields),
                CreatedAt = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application submitted. Awaiting moderator review.";
            return RedirectToAction("MyApplication");
        }

        // ----- Delete pending/approved application -----
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await _context.EditApplications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (app == null) return NotFound();

            if (app.Status != ApplicationStatus.Pending && app.Status != ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "Only pending or approved applications can be deleted.";
                return RedirectToAction("MyApplication");
            }

            _context.EditApplications.Remove(app);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application deleted. You can now submit a new one.";
            return RedirectToAction("MyApplication");
        }

        // ----- Edit fields after approval -----
        [HttpGet]
        public async Task<IActionResult> EditFields(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.IsBlocked) { TempData["ErrorMessage"] = "Blocked users cannot edit."; return RedirectToAction("MyApplication"); }

            var app = await _context.EditApplications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (app == null) return NotFound();
            if (app.Status != ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "This application is not in an editable state.";
                return RedirectToAction("MyApplication");
            }

            return View(new UserEditFieldsViewModel
            {
                ApplicationId = app.Id,
                RequestedFields = app.RequestedFields.Split(',').ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFields(UserEditFieldsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.IsBlocked) { TempData["ErrorMessage"] = "Blocked users cannot edit."; return RedirectToAction("MyApplication"); }

            var app = await _context.EditApplications.FirstOrDefaultAsync(a => a.Id == model.ApplicationId && a.UserId == user.Id);
            if (app == null) return NotFound();
            if (app.Status != ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "This application is not in an editable state.";
                return RedirectToAction("MyApplication");
            }

            var requested = app.RequestedFields.Split(',').ToList();
            var changes = new Dictionary<string, FieldChange>();

            if (requested.Contains("FullName"))
                changes["FullName"] = new FieldChange { OldValue = user.FullName, NewValue = model.FullName };
            if (requested.Contains("FatherName"))
                changes["FatherName"] = new FieldChange { OldValue = user.FatherName, NewValue = model.FatherName };
            if (requested.Contains("MotherName"))
                changes["MotherName"] = new FieldChange { OldValue = user.MotherName, NewValue = model.MotherName };
            if (requested.Contains("DateOfBirth"))
                changes["DateOfBirth"] = new FieldChange
                {
                    OldValue = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                    NewValue = model.DateOfBirth?.ToString("yyyy-MM-dd")
                };
            if (requested.Contains("Gender"))
                changes["Gender"] = new FieldChange { OldValue = user.Gender, NewValue = model.Gender };
            if (requested.Contains("Nationality"))
                changes["Nationality"] = new FieldChange { OldValue = user.Nationality, NewValue = model.Nationality };
            if (requested.Contains("Religion"))
                changes["Religion"] = new FieldChange { OldValue = user.Religion, NewValue = model.Religion };
            if (requested.Contains("Occupation"))
                changes["Occupation"] = new FieldChange { OldValue = user.Occupation, NewValue = model.Occupation };
            if (requested.Contains("BloodGroup"))
                changes["BloodGroup"] = new FieldChange { OldValue = user.BloodGroup, NewValue = model.BloodGroup };
            if (requested.Contains("PresentAddress"))
                changes["PresentAddress"] = new FieldChange { OldValue = user.PresentAddress, NewValue = model.PresentAddress };
            if (requested.Contains("PermanentAddress"))
                changes["PermanentAddress"] = new FieldChange { OldValue = user.PermanentAddress, NewValue = model.PermanentAddress };

            if (requested.Contains("PhotoPath") && model.Photo != null)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                {
                    ModelState.AddModelError("Photo", "Only JPG, JPEG, or PNG files are allowed.");
                    model.RequestedFields = requested;
                    return View(model);
                }
                var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(model.Photo.FileName);
                var filePath = Path.Combine(folder, fileName);
                using (var fs = new FileStream(filePath, FileMode.Create))
                    await model.Photo.CopyToAsync(fs);

                changes["PhotoPath"] = new FieldChange
                {
                    OldValue = user.PhotoPath,
                    NewValue = "/uploads/" + fileName
                };
            }

            app.EditedDataJson = System.Text.Json.JsonSerializer.Serialize(changes);
            app.EditedAt = DateTime.UtcNow;
            app.Status = ApplicationStatus.Edited;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Edits submitted. Awaiting moderator review.";
            return RedirectToAction("MyApplication");
        }
    }
}