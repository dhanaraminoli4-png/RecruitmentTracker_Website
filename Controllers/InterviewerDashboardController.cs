using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "Interviewer")]
    public class InterviewerDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewerDashboardController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            // Profile must be completed first.
            if (!user.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    "Setup",
                    "InterviewerProfile");
            }


            return View(user);
        }
    }
}