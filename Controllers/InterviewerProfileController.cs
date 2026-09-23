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


            if (user.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    nameof(Profile));
            }


            var model =
                BuildProfileViewModel(user);


            return View(model);
        }


        // =========================================================
        // SAVE PROFILE SETUP
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


            // Email must always come from Identity.
            model.Email =
                user.Email ?? "";


            // Remove any existing Email validation error
            // because Email is not being edited by the interviewer.
            ModelState.Remove(
                nameof(model.Email));


            if (!ModelState.IsValid)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return View(model);
            }


            // =====================================================
            // PROFILE IMAGE
            // =====================================================

            var imageSaved =
                await SaveProfileImageAsync(
                    user,
                    model);


            if (!imageSaved)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return View(model);
            }


            // =====================================================
            // APPLY PROFILE DATA
            // =====================================================

            ApplyProfileChanges(
                user,
                model);


            user.InterviewerProfileCompleted =
                true;


            // =====================================================
            // SAVE DATABASE CHANGES
            // =====================================================

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
                "Your interviewer profile has been completed successfully.";


            return RedirectToAction(
                nameof(Profile));
        }


        // =========================================================
        // VIEW PROFILE
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


        // =========================================================
        // EDIT PROFILE PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit()
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


            var model =
                BuildProfileViewModel(user);


            return View(model);
        }


        // =========================================================
        // SAVE EDITED PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            InterviewerProfileViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            // =====================================================
            // EMAIL
            // =====================================================

            // Never trust the posted email.
            // Always use the authenticated Identity user's email.
            model.Email =
                user.Email ?? "";


            // Important:
            // remove any Email validation error that may already
            // have been added during model binding.
            ModelState.Remove(
                nameof(model.Email));


            // =====================================================
            // VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return View(model);
            }


            // =====================================================
            // PROFILE IMAGE
            // =====================================================

            var imageSaved =
                await SaveProfileImageAsync(
                    user,
                    model);


            if (!imageSaved)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return View(model);
            }


            // =====================================================
            // UPDATE PROFILE DATA
            // =====================================================

            ApplyProfileChanges(
                user,
                model);


            user.InterviewerProfileCompleted =
                true;


            // =====================================================
            // SAVE DATABASE CHANGES
            // =====================================================

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
                "Your profile has been updated successfully.";


            return RedirectToAction(
                nameof(Profile));
        }


        // =========================================================
        // BUILD PROFILE VIEW MODEL
        // =========================================================

        private InterviewerProfileViewModel BuildProfileViewModel(
            ApplicationUser user)
        {
            return new InterviewerProfileViewModel
            {
                FullName =
                    user.FullName ?? "",

                DateOfBirth =
                    user.DateOfBirth,

                Gender =
                    user.Gender ?? "",

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
        }


        // =========================================================
        // APPLY PROFILE CHANGES
        // =========================================================

        private void ApplyProfileChanges(
            ApplicationUser user,
            InterviewerProfileViewModel model)
        {
            // =====================================================
            // PERSONAL
            // =====================================================

            user.FullName =
                model.FullName?.Trim();

            user.DateOfBirth =
                model.DateOfBirth;

            user.Gender =
                model.Gender?.Trim();

            user.PhoneNumber =
                model.PhoneNumber?.Trim();


            // =====================================================
            // PROFESSIONAL
            // =====================================================

            user.Department =
                model.Department?.Trim();

            user.JobTitle =
                model.JobTitle?.Trim();

            user.SeniorityLevel =
                model.SeniorityLevel?.Trim();

            user.YearsOfExperience =
                model.YearsOfExperience;

            user.Skills =
                model.Skills?.Trim();


            // =====================================================
            // DO NOT UPDATE IDENTITY EMAIL
            // =====================================================
            //
            // user.Email
            // user.UserName
            //
            // are intentionally left unchanged.
        }


        // =========================================================
        // PROFILE IMAGE UPLOAD
        // =========================================================

        private async Task<bool> SaveProfileImageAsync(
            ApplicationUser user,
            InterviewerProfileViewModel model)
        {
            // No new image uploaded.
            // Keep the current one.
            if (
                model.ProfileImage == null ||
                model.ProfileImage.Length == 0)
            {
                model.ExistingProfileImage =
                    user.ProfileImagePath;


                return true;
            }


            // =====================================================
            // ALLOWED EXTENSIONS
            // =====================================================

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


            if (!allowedExtensions.Contains(
                extension))
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImage),
                    "Please upload a JPG, JPEG, PNG or WEBP image.");


                return false;
            }


            // =====================================================
            // FILE SIZE
            // =====================================================

            const long maximumFileSize =
                2 * 1024 * 1024;


            if (
                model.ProfileImage.Length >
                maximumFileSize)
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImage),
                    "Profile picture must be 2MB or smaller.");


                return false;
            }


            // =====================================================
            // UPLOAD FOLDER
            // =====================================================

            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profiles");


            Directory.CreateDirectory(
                folder);


            // =====================================================
            // CREATE UNIQUE FILE NAME
            // =====================================================

            var fileName =
                $"{user.Id}_{Guid.NewGuid()}{extension}";


            var filePath =
                Path.Combine(
                    folder,
                    fileName);


            // =====================================================
            // SAVE FILE
            // =====================================================

            using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
            {
                await model.ProfileImage
                    .CopyToAsync(
                        stream);
            }


            // =====================================================
            // DELETE OLD IMAGE
            // =====================================================

            if (
                !string.IsNullOrWhiteSpace(
                    user.ProfileImagePath))
            {
                try
                {
                    var oldRelativePath =
                        user.ProfileImagePath
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar);


                    var oldFilePath =
                        Path.Combine(
                            _environment.WebRootPath,
                            oldRelativePath);


                    if (
                        System.IO.File.Exists(
                            oldFilePath))
                    {
                        System.IO.File.Delete(
                            oldFilePath);
                    }
                }
                catch
                {
                    // Do not stop the profile update
                    // if deleting the old image fails.
                }
            }


            // =====================================================
            // UPDATE IMAGE PATH
            // =====================================================

            user.ProfileImagePath =
                $"/uploads/profiles/{fileName}";


            model.ExistingProfileImage =
                user.ProfileImagePath;


            return true;
        }
    }
}