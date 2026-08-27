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
            var users = _userManager.Users.ToList();


            // =====================================================
            // SEARCH BY EMAIL
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users
                    .Where(u =>
                        !string.IsNullOrWhiteSpace(u.Email) &&
                        u.Email.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }


            // =====================================================
            // GET USER ROLES
            // =====================================================

            var userRoles =
                new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

                userRoles[user.Id] =
                    roles.FirstOrDefault()
                    ?? "No Role";
            }


            ViewBag.UserRoles = userRoles;

            // Keep search value in the view
            ViewBag.Search = search;


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
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }


            // =====================================================
            // VALID ROLES
            // =====================================================

            var validRoles = new[]
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
            // REMOVE CURRENT ROLE
            // =====================================================

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles);
            }


            // =====================================================
            // ADD NEW ROLE
            // =====================================================

            await _userManager.AddToRoleAsync(
                user,
                role);


            return RedirectToAction(nameof(Index));
        }
    }
}