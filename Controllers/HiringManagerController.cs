using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HiringManager")]
    public class HiringManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HiringManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // APPLICATIONS SENT TO HIRING MANAGER
        // ==========================================

        public async Task<IActionResult> Applications()
        {
            var applications = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                // Keep every candidate forwarded by HR visible.
                // A Hiring Manager decision must not delete/hide the application.
                .Where(a => a.HRShortlisted == true)
                .OrderByDescending(a => a.HiringManagerReviewed)
                .ThenByDescending(a => a.AIScore)
                .ToListAsync();

            return View(applications);
        }


        // ==========================================
        // VIEW ONE APPLICATION
        // ==========================================

        public async Task<IActionResult> Review(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }


        // ==========================================
        // SHORTLIST CANDIDATE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shortlist(int id, string? comments)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            application.HiringManagerReviewed = true;
            application.HiringManagerShortlisted = true;
            application.HiringManagerDecision = "Shortlisted";
            application.HiringManagerComments = comments ?? "";

            // Candidate-facing status stops at Sprint 1: Shortlisted.
            // Hiring Manager decision is stored separately for internal use.
            application.Status = "Shortlisted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Candidate has been shortlisted successfully.";

            return RedirectToAction(
                nameof(Review),
                new { id = id }
            );
        }


        // ==========================================
        // REJECT CANDIDATE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? comments)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            application.HiringManagerReviewed = true;
            application.HiringManagerShortlisted = false;
            application.HiringManagerDecision = "Rejected";
            application.HiringManagerComments = comments ?? "";

            // Do not overwrite the candidate-facing Sprint 1 status.
            // The HM decision remains available through the internal fields.
            application.Status = "Shortlisted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Candidate has been rejected.";

            return RedirectToAction(
                nameof(Review),
                new { id = id }
            );
        }
    }
}