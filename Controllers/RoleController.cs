using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        // =========================================================
        // ROLE MANAGEMENT
        // =========================================================

        public async Task<IActionResult> Index(string? search)
        {
            var users =
                _userManager.Users.ToList();


            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users
                    .Where(u =>
                        (
                            !string.IsNullOrWhiteSpace(u.Email) &&
                            u.Email.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(u.FullName) &&
                            u.FullName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .ToList();
            }


            var userRoles =
                new Dictionary<string, string>();


            foreach (var user in users)
            {
                var roles =
                    await _userManager
                        .GetRolesAsync(user);


                userRoles[user.Id] =
                    roles.FirstOrDefault()
                    ?? "No Role";
            }


            ViewBag.UserRoles =
                userRoles;

            ViewBag.Search =
                search;


            return View(users);
        }


        // =========================================================
        // ASSIGN ROLE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(
            string userId,
            string role)
        {
            var user =
                await _userManager
                    .FindByIdAsync(userId);


            if (user == null)
            {
                return NotFound();
            }


            // =====================================================
            // VALID ROLES
            // =====================================================

            var validRoles =
                new[]
                {
                    "HR",
                    "Candidate",
                    "Interviewer",
                    "HiringManager"
                };


            if (!validRoles.Contains(role))
            {
                return BadRequest();
            }


            // =====================================================
            // CURRENT ROLES
            // =====================================================

            var currentRoles =
                await _userManager
                    .GetRolesAsync(user);


            var previousRole =
                currentRoles.FirstOrDefault();


            // =====================================================
            // REMOVE OLD ROLES
            // =====================================================

            if (currentRoles.Any())
            {
                var removeResult =
                    await _userManager
                        .RemoveFromRolesAsync(
                            user,
                            currentRoles);


                if (!removeResult.Succeeded)
                {
                    TempData["Error"] =
                        "Could not remove the user's previous role.";

                    return RedirectToAction(
                        nameof(Index));
                }
            }


            // =====================================================
            // ADD NEW ROLE
            // =====================================================

            var addResult =
                await _userManager
                    .AddToRoleAsync(
                        user,
                        role);


            if (!addResult.Succeeded)
            {
                TempData["Error"] =
                    "Could not assign the new role.";

                return RedirectToAction(
                    nameof(Index));
            }


            // =====================================================
            // INTERVIEWER PROFILE SETUP
            //
            // If somebody becomes an Interviewer for the first time,
            // force them through the profile setup screen.
            // =====================================================

            if (role == "Interviewer" &&
                previousRole != "Interviewer")
            {
                user.InterviewerProfileCompleted =
                    false;


                var updateResult =
                    await _userManager
                        .UpdateAsync(user);


                if (!updateResult.Succeeded)
                {
                    TempData["Error"] =
                        "Role changed, but interviewer profile status could not be updated.";

                    return RedirectToAction(
                        nameof(Index));
                }
            }


            TempData["Success"] =
                role == "Interviewer"
                    ? "User has been assigned as an Interviewer. They will complete their interviewer profile on their next login."
                    : $"User role changed to {role} successfully.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}