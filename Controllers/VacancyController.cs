using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class VacancyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VacancyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // HR VACANCY PAGE
        // =========================================================

        public async Task<IActionResult> Index(
            string status = "active",
            string search = "",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var allVacancies = await _context.JobVacancies
                .OrderByDescending(v => v.CreatedDate)
                .ToListAsync();

            // =====================================================
            // COUNTS
            // =====================================================

            var totalCount = allVacancies.Count;

            var liveCount = allVacancies.Count(v =>
                v.IsActive &&
                v.ClosingDate.Date >= DateTime.Today);

            var archivedCount = allVacancies.Count(v =>
                !v.IsActive ||
                v.ClosingDate.Date < DateTime.Today);

            ViewBag.TotalCount = totalCount;
            ViewBag.LiveCount = liveCount;
            ViewBag.ArchivedCount = archivedCount;

            // =====================================================
            // FILTER
            // =====================================================

            var vacancies = allVacancies.AsEnumerable();

            if (status == "active")
            {
                vacancies = vacancies.Where(v =>
                    v.IsActive &&
                    v.ClosingDate.Date >= DateTime.Today);
            }
            else if (status == "closed")
            {
                vacancies = vacancies.Where(v =>
                    !v.IsActive ||
                    v.ClosingDate.Date < DateTime.Today);
            }

            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                vacancies = vacancies.Where(v =>
                    (v.JobTitle != null &&
                     v.JobTitle.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    (v.JobCode != null &&
                     v.JobCode.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    (v.Location != null &&
                     v.Location.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase)));
            }

            // =====================================================
            // DATE FILTER
            // =====================================================

            if (fromDate.HasValue)
            {
                vacancies = vacancies.Where(v =>
                    v.CreatedDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                vacancies = vacancies.Where(v =>
                    v.CreatedDate.Date <= toDate.Value.Date);
            }

            // =====================================================
            // APPLICATION COUNTS
            // =====================================================

            var vacancyIds = vacancies
                .Select(v => v.Id)
                .ToList();

            var applicationCounts =
                await _context.JobApplications
                    .Where(a =>
                        vacancyIds.Contains(a.JobVacancyId))
                    .GroupBy(a => a.JobVacancyId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Count());

            ViewBag.ApplicationCounts =
                applicationCounts;

            ViewBag.Status = status;
            ViewBag.Search = search;

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            return View(vacancies.ToList());
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            JobVacancy vacancy)
        {
            // =====================================================
            // VALIDATE CLOSING DATE
            // =====================================================

            if (vacancy.ClosingDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(vacancy.ClosingDate),
                    "Closing date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                return View(vacancy);
            }

            // =====================================================
            // CREATE DATE
            // =====================================================

            vacancy.CreatedDate = DateTime.Now;

            // =====================================================
            // GENERATE JOB CODE
            // =====================================================

            var lastVacancy =
                await _context.JobVacancies
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastVacancy != null &&
                !string.IsNullOrEmpty(lastVacancy.JobCode))
            {
                var numberPart =
                    lastVacancy.JobCode.Replace("JOB-", "");

                if (int.TryParse(
                    numberPart,
                    out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            vacancy.JobCode =
                $"JOB-{nextNumber:D4}";

            // =====================================================
            // SAVE
            // =====================================================

            _context.JobVacancies.Add(vacancy);

            await _context.SaveChangesAsync();

            if (vacancy.IsActive)
            {
                TempData["Success"] =
                    $"Vacancy {vacancy.JobCode} created and published successfully.";
            }
            else
            {
                TempData["Success"] =
                    $"Vacancy {vacancy.JobCode} created successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vacancy =
                await _context.JobVacancies
                    .FindAsync(id);

            if (vacancy == null)
            {
                return NotFound();
            }

            return View(vacancy);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            JobVacancy vacancy)
        {
            if (id != vacancy.Id)
            {
                return NotFound();
            }

            // =====================================================
            // VALIDATE CLOSING DATE
            // =====================================================

            if (vacancy.ClosingDate.Date < DateTime.Today &&
                vacancy.IsActive)
            {
                ModelState.AddModelError(
                    nameof(vacancy.ClosingDate),
                    "An active vacancy must have a current or future closing date.");
            }

            if (!ModelState.IsValid)
            {
                return View(vacancy);
            }

            // =====================================================
            // GET EXISTING VACANCY
            // =====================================================

            var existing =
                await _context.JobVacancies
                    .FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            // =====================================================
            // UPDATE VACANCY INFORMATION
            // =====================================================

            existing.JobTitle =
                vacancy.JobTitle;

            existing.Department =
                vacancy.Department;

            existing.Description =
                vacancy.Description;

            existing.Responsibilities =
                vacancy.Responsibilities;

            existing.Requirements =
                vacancy.Requirements;

            existing.PreferredRequirements =
                vacancy.PreferredRequirements;

            existing.Location =
                vacancy.Location;

            existing.EmploymentType =
                vacancy.EmploymentType;

            existing.Salary =
                vacancy.Salary;

            existing.ClosingDate =
                vacancy.ClosingDate;

            // =====================================================
            // COVER LETTER SETTING
            // =====================================================

            existing.RequiresCoverLetter =
                vacancy.RequiresCoverLetter;

            // =====================================================
            // KEEP THIS VACANCY'S STATUS ONLY
            //
            // Editing one vacancy must not change
            // the status of other vacancies.
            // =====================================================

            existing.IsActive =
                vacancy.IsActive;

            // =====================================================
            // SAVE
            // =====================================================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Vacancy updated successfully.";

            return RedirectToAction(
                nameof(Manage),
                new { id });
        }

        // =========================================================
        // PUBLISH / UNPUBLISH
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var vacancy =
                await _context.JobVacancies
                    .FirstOrDefaultAsync(v => v.Id == id);

            if (vacancy == null)
            {
                return NotFound();
            }

            // =====================================================
            // CHANGE ONLY THE SELECTED VACANCY
            //
            // Other published vacancies are not changed.
            // =====================================================

            vacancy.IsActive =
                !vacancy.IsActive;

            await _context.SaveChangesAsync();

            if (vacancy.IsActive)
            {
                TempData["Success"] =
                    $"Vacancy {vacancy.JobCode} has been published.";
            }
            else
            {
                TempData["Success"] =
                    $"Vacancy {vacancy.JobCode} has been unpublished.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacancy =
                await _context.JobVacancies
                    .FirstOrDefaultAsync(v => v.Id == id);

            if (vacancy == null)
            {
                return NotFound();
            }

            return View(vacancy);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vacancy =
                await _context.JobVacancies
                    .FindAsync(id);

            if (vacancy != null)
            {
                // =================================================
                // CHECK APPLICATIONS
                // =================================================

                var hasApplications =
                    await _context.JobApplications
                        .AnyAsync(a =>
                            a.JobVacancyId == id);

                if (hasApplications)
                {
                    TempData["Error"] =
                        "This vacancy has applications. " +
                        "Unpublish it instead so the recruitment history is preserved.";

                    return RedirectToAction(nameof(Index));
                }

                // =================================================
                // DELETE
                // =================================================

                _context.JobVacancies.Remove(vacancy);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Vacancy deleted permanently.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MANAGE ONE VACANCY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Manage(int id)
        {
            var vacancy =
                await _context.JobVacancies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == id);

            if (vacancy == null)
            {
                return NotFound();
            }

            var applications =
                await _context.JobApplications
                    .AsNoTracking()
                    .Where(a =>
                        a.JobVacancyId == id)
                    .ToListAsync();

            ViewBag.ApplicationCount =
                applications.Count;

            ViewBag.NeedsReviewCount =
                applications.Count(a =>
                    !a.HRReviewed &&
                    a.Status != "Not Shortlisted");

            ViewBag.PassedCount =
                applications.Count(a =>
                    a.AIScore >= 70 &&
                    a.Status != "Not Shortlisted");

            ViewBag.ReviewCount =
                applications.Count(a =>
                    a.AIScore >= 50 &&
                    a.AIScore < 70 &&
                    a.Status != "Not Shortlisted");

            ViewBag.FailedCount =
                applications.Count(a =>
                    a.AIScore < 50 &&
                    a.Status != "Not Shortlisted");

            ViewBag.ShortlistedCount =
                applications.Count(a =>
                    a.HRShortlisted);

            ViewBag.NotShortlistedCount =
                applications.Count(a =>
                    a.Status == "Not Shortlisted");

            return View(vacancy);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vacancy =
                await _context.JobVacancies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        v => v.Id == id);

            if (vacancy == null)
            {
                return NotFound();
            }

            return View(vacancy);
        }
    }
}