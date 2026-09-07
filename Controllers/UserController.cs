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

            byte[]? photoBytes = null;
            if (!string.IsNullOrEmpty(user.PhotoPath))
            {
                var photoPath = Path.Combine(_webHostEnvironment.WebRootPath, user.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(photoPath))
                {
                    try
                    {
                        // Decode with SkiaSharp, then re-encode as PNG
                        using var bitmap = SkiaSharp.SKBitmap.Decode(photoPath);
                        if (bitmap != null)
                        {
                            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
                            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            photoBytes = data.ToArray();
                        }
                    }
                    catch
                    {
                        // If decoding fails, photoBytes stays null
                    }
                }
            }
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Credit card size: 85.6mm x 54mm
                    page.Size(new PageSize(242.65f, 153.07f, Unit.Point));
                    page.Margin(0); // No outer margin; we'll use padding inside content
                    page.DefaultTextStyle(x => x.FontSize(7));

                    // Page background color
                    page.PageColor(Colors.Green.Lighten4);

                    // Single content layer with border and padding
                    page.Content()
                        .Padding(10)
                        .Border(1.5f)
                        .BorderColor(Colors.Green.Darken3)
                        .Row(row =>
                        {
                            // Left: Photo
                            row.ConstantItem(80).Column(photoCol =>
                            {
                                photoCol.Item().AlignCenter().AlignMiddle().Width(70).Height(90)
                                    .Border(1).BorderColor(Colors.Grey.Darken1)
                                    .Background(Colors.White)
                                    .Column(inner =>
                                    {
                                        if (photoBytes != null)
                                        {
                                            inner.Item().Image(photoBytes).FitArea();
                                        }
                                        else
                                        {
                                            inner.Item().AlignCenter().AlignMiddle().Text("No Photo").FontSize(8);
                                        }
                                    });
                                photoCol.Item().AlignCenter().Text("Photo").FontSize(6).SemiBold();
                            });

                            // Right: Details
                            row.RelativeItem().PaddingLeft(10).Column(infoCol =>
                            {
                                infoCol.Item().AlignCenter().Text("National Identity Card")
                                    .FontSize(9).SemiBold().FontColor(Colors.Green.Darken3);
                                infoCol.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Green.Darken1);

                                infoCol.Item().PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Text("NID No:").SemiBold();
                                    r.RelativeItem(2).Text(user.NIDNumber ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Name:").SemiBold();
                                    r.RelativeItem(2).Text(user.FullName);
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Father:").SemiBold();
                                    r.RelativeItem(2).Text(user.FatherName ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Mother:").SemiBold();
                                    r.RelativeItem(2).Text(user.MotherName ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("DOB:").SemiBold();
                                    r.RelativeItem(2).Text(user.DateOfBirth?.ToString("dd MMM yyyy") ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Gender:").SemiBold();
                                    r.RelativeItem(2).Text(user.Gender ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Blood:").SemiBold();
                                    r.RelativeItem(2).Text(user.BloodGroup ?? "N/A");
                                });
                                infoCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Address:").SemiBold();
                                    r.RelativeItem(2).Text(user.PresentAddress ?? "N/A");
                                });
                            });
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}