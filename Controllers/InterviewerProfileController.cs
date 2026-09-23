using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "Interviewer")]
    public class InterviewerProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public InterviewerProfileController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _environment = environment;
        }


        // =========================================================
        // PROFILE SETUP PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            // Already completed?
            // Go to dashboard instead.
            if (user.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            var model =
                new InterviewerProfileViewModel
                {
                    FullName =
                        user.FullName ?? "",

                    DateOfBirth =
                        user.DateOfBirth,

                    Gender =
                        user.Gender ?? "",

                    // Display only.
                    // We are NOT using email to save/profile-match.
                    Email =
                        user.Email ?? "",

                    PhoneNumber =
                        user.PhoneNumber ?? "",

                    ExistingProfileImage =
                        user.ProfileImagePath,

                    Department =
                        user.Department ?? "",

                    JobTitle =
                        user.JobTitle ?? "",

                    SeniorityLevel =
                        user.SeniorityLevel ?? "",

                    YearsOfExperience =
                        user.YearsOfExperience,

                    Skills =
                        user.Skills ?? ""
                };


            return View(model);
        }


        // =========================================================
        // SAVE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(
            InterviewerProfileViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            // Email comes from the logged-in Identity account.
            model.Email =
                user.Email ?? "";


            if (!ModelState.IsValid)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;

                return View(model);
            }


            // =====================================================
            // PROFILE IMAGE
            // =====================================================

            if (model.ProfileImage != null &&
                model.ProfileImage.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                var extension =
                    Path.GetExtension(
                        model.ProfileImage.FileName)
                    .ToLowerInvariant();


                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.ProfileImage),
                        "Please upload a JPG, PNG or WEBP image.");


                    model.ExistingProfileImage =
                        user.ProfileImagePath;


                    return View(model);
                }


                // Optional 2MB limit
                if (model.ProfileImage.Length >
                    2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfileImage),
                        "Profile picture must be 2MB or smaller.");


                    model.ExistingProfileImage =
                        user.ProfileImagePath;


                    return View(model);
                }


                var folder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "profiles");


                Directory.CreateDirectory(
                    folder);


                var fileName =
                    $"{user.Id}_{Guid.NewGuid()}{extension}";


                var filePath =
                    Path.Combine(
                        folder,
                        fileName);


                using (
                    var stream =
                        new FileStream(
                            filePath,
                            FileMode.Create))
                {
                    await model.ProfileImage
                        .CopyToAsync(stream);
                }


                user.ProfileImagePath =
                    $"/uploads/profiles/{fileName}";
            }


            // =====================================================
            // PERSONAL INFORMATION
            // =====================================================

            user.FullName =
                model.FullName.Trim();


            user.DateOfBirth =
                model.DateOfBirth;


            user.Gender =
                model.Gender.Trim();


            user.PhoneNumber =
                model.PhoneNumber.Trim();


            // IMPORTANT:
            //
            // We do NOT modify:
            // user.Email
            // user.UserName
            //
            // The existing Identity account remains unchanged.


            // =====================================================
            // PROFESSIONAL / ELIGIBILITY INFORMATION
            // =====================================================

            user.Department =
                model.Department.Trim();


            user.JobTitle =
                model.JobTitle.Trim();


            user.SeniorityLevel =
                model.SeniorityLevel.Trim();


            user.YearsOfExperience =
                model.YearsOfExperience;


            user.Skills =
                model.Skills.Trim();


            // =====================================================
            // PROFILE COMPLETED
            // =====================================================

            user.InterviewerProfileCompleted =
                true;


            var result =
                await _userManager.UpdateAsync(
                    user);


            if (!result.Succeeded)
            {
                foreach (
                    var error
                    in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }


                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return View(model);
            }


            TempData["Success"] =
                "Your interviewer profile has been completed.";


            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        // =========================================================
        // VIEW INTERVIEWER PROFILE
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

            if (!user.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    nameof(Setup));
            }

            return View(user);
        }
    }
}