using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class CandidateProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;


        public CandidateProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }



        // =========================================================
        // MY PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            var profile =
                await _context.CandidateProfiles
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == user.Id);


            // Candidate has never created a profile before.
            if (profile == null)
            {
                profile =
                    new CandidateProfile
                    {
                        CandidateId = user.Id,

                        CreatedDate =
                            DateTime.Now,

                        UpdatedDate =
                            DateTime.Now
                    };


                _context.CandidateProfiles.Add(
                    profile
                );


                await _context.SaveChangesAsync();
            }


            var model =
                new CandidateProfileViewModel
                {
                    Id =
                        profile.Id,

                    FullName =
                        user.FullName
                        ?? "",

                    Email =
                        user.Email
                        ?? "",

                    PhoneNumber =
                        profile.PhoneNumber,

                    Address =
                        profile.Address,

                    City =
                        profile.City,

                    Country =
                        profile.Country,

                    CurrentPosition =
                        profile.CurrentPosition,

                    Education =
                        profile.Education,

                    Skills =
                        profile.Skills,

                    Experience =
                        profile.Experience,

                    LinkedInUrl =
                        profile.LinkedInUrl,

                    GitHubUrl =
                        profile.GitHubUrl,

                    PortfolioUrl =
                        profile.PortfolioUrl,

                    ProfileImagePath =
                        profile.ProfileImagePath,

                    DefaultCVPath =
                        profile.DefaultCVPath,

                    DefaultCVFileName =
                        profile.DefaultCVFileName
                };


            return View(model);
        }



        // =========================================================
        // SAVE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            CandidateProfileViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            var profile =
                await _context.CandidateProfiles
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == user.Id);


            if (profile == null)
            {
                profile =
                    new CandidateProfile
                    {
                        CandidateId =
                            user.Id,

                        CreatedDate =
                            DateTime.Now
                    };


                _context.CandidateProfiles.Add(
                    profile
                );
            }



            // =====================================================
            // UPDATE ACCOUNT NAME
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                model.FullName))
            {
                user.FullName =
                    model.FullName.Trim();


                await _userManager.UpdateAsync(
                    user
                );
            }



            // =====================================================
            // PROFILE DATA
            // =====================================================

            profile.PhoneNumber =
                model.PhoneNumber?.Trim();

            profile.Address =
                model.Address?.Trim();

            profile.City =
                model.City?.Trim();

            profile.Country =
                model.Country?.Trim();

            profile.CurrentPosition =
                model.CurrentPosition?.Trim();

            profile.Education =
                model.Education?.Trim();

            profile.Skills =
                model.Skills?.Trim();

            profile.Experience =
                model.Experience?.Trim();

            profile.LinkedInUrl =
                model.LinkedInUrl?.Trim();

            profile.GitHubUrl =
                model.GitHubUrl?.Trim();

            profile.PortfolioUrl =
                model.PortfolioUrl?.Trim();



            // =====================================================
            // PROFILE IMAGE
            // =====================================================

            if (model.ProfileImage != null &&
                model.ProfileImage.Length > 0)
            {
                var imageExtension =
                    Path.GetExtension(
                        model.ProfileImage.FileName
                    )
                    .ToLowerInvariant();


                var allowedImages =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                if (!allowedImages.Contains(
                    imageExtension))
                {
                    TempData["Error"] =
                        "Profile photo must be JPG, JPEG, PNG or WEBP.";


                    return RedirectToAction(
                        nameof(Profile)
                    );
                }


                var imageFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "candidate-profiles",
                        "images"
                    );


                Directory.CreateDirectory(
                    imageFolder
                );


                var imageName =
                    $"{user.Id}_{Guid.NewGuid():N}{imageExtension}";


                var imagePath =
                    Path.Combine(
                        imageFolder,
                        imageName
                    );


                await using (
                    var stream =
                        new FileStream(
                            imagePath,
                            FileMode.Create))
                {
                    await model.ProfileImage
                        .CopyToAsync(stream);
                }


                profile.ProfileImagePath =
                    $"/uploads/candidate-profiles/images/{imageName}";
            }



            // =====================================================
            // DEFAULT CV
            // =====================================================

            if (model.CVFile != null &&
                model.CVFile.Length > 0)
            {
                var cvExtension =
                    Path.GetExtension(
                        model.CVFile.FileName
                    )
                    .ToLowerInvariant();


                var allowedCVTypes =
                    new[]
                    {
                        ".pdf",
                        ".doc",
                        ".docx"
                    };


                if (!allowedCVTypes.Contains(
                    cvExtension))
                {
                    TempData["Error"] =
                        "CV must be a PDF, DOC or DOCX file.";


                    return RedirectToAction(
                        nameof(Profile)
                    );
                }


                const long maxFileSize =
                    10 * 1024 * 1024;


                if (model.CVFile.Length >
                    maxFileSize)
                {
                    TempData["Error"] =
                        "CV cannot be larger than 10 MB.";


                    return RedirectToAction(
                        nameof(Profile)
                    );
                }


                var cvFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "candidate-profiles",
                        "cvs"
                    );


                Directory.CreateDirectory(
                    cvFolder
                );


                var cvName =
                    $"{user.Id}_{Guid.NewGuid():N}{cvExtension}";


                var cvPath =
                    Path.Combine(
                        cvFolder,
                        cvName
                    );


                await using (
                    var stream =
                        new FileStream(
                            cvPath,
                            FileMode.Create))
                {
                    await model.CVFile
                        .CopyToAsync(stream);
                }


                profile.DefaultCVPath =
                    $"/uploads/candidate-profiles/cvs/{cvName}";


                profile.DefaultCVFileName =
                    Path.GetFileName(
                        model.CVFile.FileName
                    );
            }



            // =====================================================
            // PROFILE COMPLETION
            // =====================================================

            profile.IsProfileComplete =
                !string.IsNullOrWhiteSpace(
                    user.FullName) &&

                !string.IsNullOrWhiteSpace(
                    profile.PhoneNumber) &&

                !string.IsNullOrWhiteSpace(
                    profile.DefaultCVPath);



            profile.UpdatedDate =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Your candidate profile has been updated successfully.";


            return RedirectToAction(
                nameof(Profile)
            );
        }



        // =========================================================
        // VIEW DEFAULT CV
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewCV()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            var profile =
                await _context.CandidateProfiles
                    .FirstOrDefaultAsync(x =>
                        x.CandidateId == user.Id);


            if (profile == null ||
                string.IsNullOrWhiteSpace(
                    profile.DefaultCVPath))
            {
                return NotFound(
                    "No CV has been uploaded."
                );
            }


            var relativePath =
                profile.DefaultCVPath
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    );


            var fullPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath
                );


            if (!System.IO.File.Exists(
                fullPath))
            {
                return NotFound(
                    "CV file could not be found."
                );
            }


            var extension =
                Path.GetExtension(
                    fullPath
                )
                .ToLowerInvariant();


            var contentType =
                extension switch
                {
                    ".pdf" =>
                        "application/pdf",

                    ".doc" =>
                        "application/msword",

                    ".docx" =>
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                    _ =>
                        "application/octet-stream"
                };


            return PhysicalFile(
                fullPath,
                contentType,
                enableRangeProcessing: true
            );
        }
    }
}