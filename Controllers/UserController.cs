using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public UserController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
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
    }
}