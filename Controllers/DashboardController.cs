using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // MAIN ROLE REDIRECT
        // =========================================================

        public async Task<IActionResult> Index()
        {
            // =============================================
            // SYSTEM ADMIN
            // =============================================

            if (User.IsInRole("SystemAdmin"))
            {
                return RedirectToAction(
                    nameof(SystemAdmin));
            }


            // =============================================
            // HR
            // =============================================

            if (User.IsInRole("HR"))
            {
                return RedirectToAction(
                    nameof(HR));
            }


            // =============================================
            // INTERVIEWER
            //
            // IMPORTANT:
            // Check Interviewer BEFORE Candidate.
            // A user may originally have been Candidate
            // before Admin promotes them.
            // =============================================

            if (User.IsInRole("Interviewer"))
            {
                var user =
                    await _userManager.GetUserAsync(User);


                if (user == null)
                {
                    return Challenge();
                }


                // First login after becoming Interviewer
                if (!user.InterviewerProfileCompleted)
                {
                    return RedirectToAction(
                        "Setup",
                        "InterviewerProfile");
                }


                return RedirectToAction(
                    nameof(Interviewer));
            }


            // =============================================
            // HIRING MANAGER
            // =============================================

            if (User.IsInRole("HiringManager"))
            {
                return RedirectToAction(
                    nameof(Manager));
            }


            // =============================================
            // CANDIDATE
            // =============================================

            if (User.IsInRole("Candidate"))
            {
                return RedirectToAction(
                    nameof(Candidate));
            }


            // No known role
            return View();
        }



        // =========================================================
        // SYSTEM ADMIN DASHBOARD
        // =========================================================

        [Authorize(Roles = "SystemAdmin")]
        public IActionResult SystemAdmin()
        {
            return View();
        }



        // =========================================================
        // HR DASHBOARD
        // =========================================================

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HR()
        {
            var today =
                DateTime.Today;


            var applications =
                _context.JobApplications
                    .AsNoTracking();


            var vacancies =
                _context.JobVacancies
                    .AsNoTracking();


            ViewBag.ActiveVacancies =
                await vacancies.CountAsync(
                    v =>
                        v.IsActive &&
                        v.ClosingDate.Date >= today);


            ViewBag.ClosingSoon =
                await vacancies.CountAsync(
                    v =>
                        v.IsActive &&
                        v.ClosingDate.Date >= today &&
                        v.ClosingDate.Date <=
                        today.AddDays(7));


            ViewBag.TotalApplications =
                await applications.CountAsync();


            ViewBag.NeedsReview =
                await applications.CountAsync(
                    a =>
                        !a.HRReviewed);


            ViewBag.Shortlisted =
                await applications.CountAsync(
                    a =>
                        a.HRShortlisted);


            ViewBag.NotShortlisted =
                await applications.CountAsync(
                    a =>
                        a.Status ==
                        "Not Shortlisted");


            ViewBag.SentToHM =
                await applications.CountAsync(
                    a =>
                        a.HRShortlisted);


            ViewBag.RecentVacancies =
                await vacancies

                    .OrderByDescending(
                        v =>
                            v.CreatedDate)

                    .Take(5)

                    .ToListAsync();


            return View();
        }



        // =========================================================
        // CANDIDATE DASHBOARD
        // =========================================================

        [Authorize(Roles = "Candidate")]
        public IActionResult Candidate()
        {
            return View();
        }



        // =========================================================
        // INTERVIEWER DASHBOARD
        // =========================================================

        [Authorize(Roles = "Interviewer")]
        public async Task<IActionResult> Interviewer()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return Challenge();
            }


            // Even if the user manually types:
            // /Dashboard/Interviewer
            // they must complete setup first.
            if (!user.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    "Setup",
                    "InterviewerProfile");
            }


            return View(user);
        }



        // =========================================================
        // HIRING MANAGER DASHBOARD
        // =========================================================

        [Authorize(Roles = "HiringManager")]
        public IActionResult Manager()
        {
            return View();
        }
    }
}