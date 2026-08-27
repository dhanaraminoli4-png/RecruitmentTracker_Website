using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;

namespace RecruitmentTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("SystemAdmin")) return RedirectToAction("SystemAdmin");
            if (User.IsInRole("HR")) return RedirectToAction("HR");
            if (User.IsInRole("Candidate")) return RedirectToAction("Candidate");
            if (User.IsInRole("Interviewer")) return RedirectToAction("Interviewer");
            if (User.IsInRole("HiringManager")) return RedirectToAction("Manager");
            return View();
        }

        [Authorize(Roles = "SystemAdmin")]
        public IActionResult SystemAdmin() => View();

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HR()
        {
            var today = DateTime.Today;
            var applications = _context.JobApplications.AsNoTracking();
            var vacancies = _context.JobVacancies.AsNoTracking();

            ViewBag.ActiveVacancies = await vacancies.CountAsync(v => v.IsActive && v.ClosingDate.Date >= today);
            ViewBag.ClosingSoon = await vacancies.CountAsync(v => v.IsActive && v.ClosingDate.Date >= today && v.ClosingDate.Date <= today.AddDays(7));
            ViewBag.TotalApplications = await applications.CountAsync();
            ViewBag.NeedsReview = await applications.CountAsync(a => !a.HRReviewed);
            ViewBag.Shortlisted = await applications.CountAsync(a => a.HRShortlisted);
            ViewBag.NotShortlisted = await applications.CountAsync(a => a.Status == "Not Shortlisted");
            ViewBag.SentToHM = await applications.CountAsync(a => a.HRShortlisted);
            ViewBag.RecentVacancies = await vacancies
                .OrderByDescending(v => v.CreatedDate)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Candidate")]
        public IActionResult Candidate() => View();

        [Authorize(Roles = "Interviewer")]
        public IActionResult Interviewer() => View();

        [Authorize(Roles = "HiringManager")]
        public IActionResult Manager() => View();
    }
}
