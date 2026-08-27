using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.Json;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HRApplicationController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // ============================================================
        // HR APPLICATION MANAGEMENT
        // ============================================================

        public async Task<IActionResult> Management(
            string status = "all",
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            double? minScore = null)
        {
            var baseQuery = _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .AsNoTracking()
                .AsQueryable();


            // ========================================================
            // SEARCH
            // ========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                baseQuery = baseQuery.Where(a =>
                    (a.Candidate != null &&
                     (a.Candidate.FullName ?? "")
                        .ToLower()
                        .Contains(term))

                    ||

                    (a.Candidate != null &&
                     (a.Candidate.Email ?? "")
                        .ToLower()
                        .Contains(term))

                    ||

                    (a.JobVacancy != null &&
                     (a.JobVacancy.JobTitle ?? "")
                        .ToLower()
                        .Contains(term))

                    ||

                    (a.JobVacancy != null &&
                     (a.JobVacancy.JobCode ?? "")
                        .ToLower()
                        .Contains(term))
                );
            }


            // ========================================================
            // DATE FILTER
            // ========================================================

            if (fromDate.HasValue)
            {
                baseQuery = baseQuery.Where(a =>
                    a.AppliedDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                baseQuery = baseQuery.Where(a =>
                    a.AppliedDate.Date <= toDate.Value.Date);
            }


            // ========================================================
            // MINIMUM SCORE FILTER
            // ========================================================

            if (minScore.HasValue)
            {
                baseQuery = baseQuery.Where(a =>
                    a.AIScore >= minScore.Value);
            }


            // ========================================================
            // NORMALISE STATUS
            // ========================================================

            status = string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.ToLowerInvariant();


            // ========================================================
            // GET ALL APPLICATIONS
            // ========================================================

            var allApplications = await baseQuery
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToListAsync();


            // ========================================================
            // COUNTS
            // ========================================================

            ViewBag.AllCount = allApplications.Count;


            // Passed = TOP 15 AI-ranked candidates PER VACANCY
            //
            // IMPORTANT:
            // Candidates manually passed from Review are also included
            // if their Status is "Passed".
            // ========================================================

            var automaticPassedIds = GetAutomaticTop15Ids(
                allApplications);


            ViewBag.PassedCount = allApplications.Count(a =>
                automaticPassedIds.Contains(a.Id) ||
                (
                    a.Status == "Passed" &&
                    a.Status != "Not Shortlisted"
                ));


            // ========================================================
            // REVIEW COUNT
            // ========================================================
            //
            // Review contains:
            //
            // 1. Candidates with 70+ who are NOT in the top 15
            // 2. Candidates between 50 and 69.99
            //
            // Manually passed candidates are excluded.
            // Failed candidates are excluded.
            // ========================================================

            ViewBag.ReviewCount = allApplications.Count(a =>
                !automaticPassedIds.Contains(a.Id) &&
                a.Status != "Passed" &&
                a.Status != "Not Shortlisted" &&
                a.AIScore >= 50
            );


            // ========================================================
            // FAILED COUNT
            // ========================================================

            ViewBag.FailedCount = allApplications.Count(a =>
                a.AIScore < 50 ||
                a.Status == "Not Shortlisted");


            // ========================================================
            // SELECTED TAB
            // ========================================================

            IEnumerable<JobApplication> filteredApplications =
                allApplications;


            switch (status)
            {
                case "passed":

                    filteredApplications = allApplications
                        .Where(a =>
                            automaticPassedIds.Contains(a.Id) ||
                            (
                                a.Status == "Passed" &&
                                a.Status != "Not Shortlisted"
                            ));

                    break;


                case "review":

                    filteredApplications = allApplications
                        .Where(a =>
                            !automaticPassedIds.Contains(a.Id) &&
                            a.Status != "Passed" &&
                            a.Status != "Not Shortlisted" &&
                            a.AIScore >= 50);

                    break;


                case "failed":

                    filteredApplications = allApplications
                        .Where(a =>
                            a.AIScore < 50 ||
                            a.Status == "Not Shortlisted");

                    break;


                case "all":

                default:

                    break;
            }


            // ========================================================
            // FINAL ORDER
            // ========================================================
            //
            // Highest score first.
            // Earlier application date breaks ties.
            // ========================================================

            var applications = filteredApplications
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToList();


            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.MinScore = minScore;


            return View(applications);
        }


        // ============================================================
        // REPORTS
        // ============================================================

        public async Task<IActionResult> Reports(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var applications =
                _context.JobApplications
                .Include(a => a.JobVacancy)
                .AsNoTracking()
                .AsQueryable();


            if (fromDate.HasValue)
            {
                applications = applications.Where(a =>
                    a.AppliedDate.Date >= fromDate.Value.Date);
            }


            if (toDate.HasValue)
            {
                applications = applications.Where(a =>
                    a.AppliedDate.Date <= toDate.Value.Date);
            }


            var applicationList = await applications
                .ToListAsync();


            // ========================================================
            // BUILD REPORT BY VACANCY
            // ========================================================

            var rows = applicationList
                .GroupBy(a => new
                {
                    a.JobVacancyId,
                    a.JobVacancy!.JobTitle,
                    a.JobVacancy.Location,
                    a.JobVacancy.CreatedDate,
                    a.JobVacancy.ClosingDate,
                    a.JobVacancy.IsActive
                })
                .Select(g =>
                {
                    var vacancyApplications = g
                        .OrderByDescending(a => a.AIScore)
                        .ThenBy(a => a.AppliedDate)
                        .ToList();

                    var top15Ids = vacancyApplications
                        .Where(a =>
                            a.AIScore >= 70 &&
                            a.Status != "Not Shortlisted")
                        .Take(15)
                        .Select(a => a.Id)
                        .ToHashSet();


                    return new
                    {
                        g.Key.JobVacancyId,
                        g.Key.JobTitle,
                        g.Key.Location,
                        g.Key.CreatedDate,
                        g.Key.ClosingDate,
                        g.Key.IsActive,

                        Applications = vacancyApplications.Count,

                        Passed = vacancyApplications.Count(a =>
                            top15Ids.Contains(a.Id) ||
                            (
                                a.Status == "Passed" &&
                                a.Status != "Not Shortlisted"
                            )),

                        HRReviewed = vacancyApplications.Count(a =>
                            a.HRReviewed),

                        Shortlisted = vacancyApplications.Count(a =>
                            top15Ids.Contains(a.Id) ||
                            a.Status == "Passed"),

                        NotShortlisted = vacancyApplications.Count(a =>
                            a.Status == "Not Shortlisted")
                    };
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToList();


            ViewBag.TotalApplications =
                rows.Sum(x => x.Applications);

            ViewBag.Passed =
                rows.Sum(x => x.Passed);

            ViewBag.HRReviewed =
                rows.Sum(x => x.HRReviewed);

            ViewBag.Shortlisted =
                rows.Sum(x => x.Shortlisted);

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");


            return View(rows);
        }


        // ============================================================
        // ALL APPLICATIONS FOR ONE VACANCY
        // ============================================================

        public async Task<IActionResult> Index(int vacancyId)
        {
            var vacancy = await _context.JobVacancies
                .FirstOrDefaultAsync(v => v.Id == vacancyId);


            if (vacancy == null)
            {
                return NotFound();
            }


            var applications = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .Where(a => a.JobVacancyId == vacancyId)
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToListAsync();


            ViewBag.VacancyId = vacancyId;
            ViewBag.VacancyTitle = vacancy.JobTitle;
            ViewBag.Title = "Applications";


            return View(applications);
        }


        // ============================================================
        // PASSED APPLICATIONS
        // ============================================================
        //
        // ONLY THE TOP 15 AI-SCORED CANDIDATES ARE SHOWN HERE.
        //
        // Ranking:
        // 1. Highest AIScore
        // 2. Earlier AppliedDate if scores are equal
        //
        // ============================================================

        public async Task<IActionResult> Passed(int vacancyId)
        {
            var vacancy = await _context.JobVacancies
                .FirstOrDefaultAsync(v => v.Id == vacancyId);


            if (vacancy == null)
            {
                return NotFound();
            }


            var applications = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .Where(a =>
                    a.JobVacancyId == vacancyId &&
                    a.AIScore >= 70 &&
                    a.Status != "Not Shortlisted")
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .Take(15)
                .ToListAsync();


            ViewBag.VacancyId = vacancyId;
            ViewBag.VacancyTitle = vacancy.JobTitle;
            ViewBag.Title = "Top 15 Candidates";


            return View(
                "FilteredApplications",
                applications);
        }


        // ============================================================
        // REVIEW APPLICATIONS
        // ============================================================
        //
        // REVIEW CONTAINS:
        //
        // - Candidates scoring 70+ but outside the top 15
        // - Candidates scoring between 50 and 69.99
        //
        // Ordered highest score to lowest score.
        //
        // ============================================================

        public async Task<IActionResult> Review(int vacancyId)
        {
            var vacancy = await _context.JobVacancies
                .FirstOrDefaultAsync(v => v.Id == vacancyId);


            if (vacancy == null)
            {
                return NotFound();
            }


            // Get all candidates for this vacancy
            var allApplications = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .Where(a =>
                    a.JobVacancyId == vacancyId &&
                    a.Status != "Not Shortlisted" &&
                    a.Status != "Passed")
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToListAsync();


            // Find the top 15
            var top15Ids = allApplications
                .Where(a => a.AIScore >= 70)
                .Take(15)
                .Select(a => a.Id)
                .ToHashSet();


            // Review = everyone who is not in the top 15
            // and has a score of at least 50.
            var applications = allApplications
                .Where(a =>
                    !top15Ids.Contains(a.Id) &&
                    a.AIScore >= 50)
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToList();


            ViewBag.VacancyId = vacancyId;
            ViewBag.VacancyTitle = vacancy.JobTitle;
            ViewBag.Title = "Candidates for Review";


            return View(
                "FilteredApplications",
                applications);
        }


        // ============================================================
        // FAILED APPLICATIONS
        // ============================================================

        public async Task<IActionResult> Failed(int vacancyId)
        {
            var vacancy = await _context.JobVacancies
                .FirstOrDefaultAsync(v => v.Id == vacancyId);


            if (vacancy == null)
            {
                return NotFound();
            }


            var applications = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .Where(a =>
                    a.JobVacancyId == vacancyId &&
                    (
                        a.AIScore < 50 ||
                        a.Status == "Not Shortlisted"
                    ))
                .OrderByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToListAsync();


            ViewBag.VacancyId = vacancyId;
            ViewBag.VacancyTitle = vacancy.JobTitle;
            ViewBag.Title = "Failed Candidates";


            return View(
                "FilteredApplications",
                applications);
        }


        // ============================================================
        // AI ANALYSIS
        // ============================================================

        public async Task<IActionResult> AIAnalysis(
            int id,
            int vacancyId)
        {
            var application =
                await _context.JobApplications
                    .Include(a => a.Candidate)
                    .Include(a => a.JobVacancy)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.JobVacancyId == vacancyId);


            if (application == null)
            {
                return NotFound();
            }


            var requiredResults =
                string.IsNullOrWhiteSpace(
                    application.AIRequiredResults)
                ? new List<AIAnalysisRequirement>()
                : JsonSerializer.Deserialize<
                    List<AIAnalysisRequirement>>(
                        application.AIRequiredResults)
                    ?? new List<AIAnalysisRequirement>();


            var preferredResults =
                string.IsNullOrWhiteSpace(
                    application.AIPreferredResults)
                ? new List<AIAnalysisRequirement>()
                : JsonSerializer.Deserialize<
                    List<AIAnalysisRequirement>>(
                        application.AIPreferredResults)
                    ?? new List<AIAnalysisRequirement>();


            var model = new AIAnalysisViewModel
            {
                Application = application,
                RequiredResults = requiredResults,
                PreferredResults = preferredResults
            };


            ViewBag.VacancyId = vacancyId;


            return View(model);
        }


        // ============================================================
        // VIEW CV
        // ============================================================

        public async Task<IActionResult> ViewCV(
            int id,
            int vacancyId)
        {
            var application =
                await _context.JobApplications
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.JobVacancyId == vacancyId);


            if (application == null)
            {
                return NotFound();
            }


            if (string.IsNullOrWhiteSpace(
                application.CVFilePath))
            {
                return NotFound(
                    "CV file was not found.");
            }


            var fullPath = GetFilePath(
                application.CVFilePath);


            if (fullPath == null)
            {
                return NotFound(
                    "CV file does not exist.");
            }


            var contentType =
                GetContentType(fullPath);


            return PhysicalFile(
                fullPath,
                contentType);
        }


        // ============================================================
        // VIEW COVER LETTER
        // ============================================================

        public async Task<IActionResult> ViewCoverLetter(
            int id,
            int vacancyId)
        {
            var application =
                await _context.JobApplications
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.JobVacancyId == vacancyId);


            if (application == null)
            {
                return NotFound();
            }


            if (string.IsNullOrWhiteSpace(
                application.CoverLetterFilePath))
            {
                return NotFound(
                    "Cover letter file was not found.");
            }


            var fullPath = GetFilePath(
                application.CoverLetterFilePath);


            if (fullPath == null)
            {
                return NotFound(
                    "Cover letter file does not exist.");
            }


            var contentType =
                GetContentType(fullPath);


            return PhysicalFile(
                fullPath,
                contentType);
        }


        // ============================================================
        // MARK SELECTED REVIEW CANDIDATES AS PASSED
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPassed(
            int vacancyId,
            List<int> selectedIds)
        {
            if (selectedIds == null ||
                selectedIds.Count == 0)
            {
                TempData["Error"] =
                    "Please select at least one candidate.";

                return RedirectToAction(
                    nameof(Review),
                    new { vacancyId });
            }


            var applications =
                await _context.JobApplications
                    .Where(a =>
                        a.JobVacancyId == vacancyId &&
                        selectedIds.Contains(a.Id) &&
                        a.AIScore >= 50 &&
                        a.Status != "Passed" &&
                        a.Status != "Not Shortlisted")
                    .ToListAsync();


            if (applications.Count == 0)
            {
                TempData["Error"] =
                    "No valid review candidates were selected.";

                return RedirectToAction(
                    nameof(Review),
                    new { vacancyId });
            }


            foreach (var application in applications)
            {
                application.HRReviewed = true;

                // This is not the Hiring Manager shortlist.
                application.HRShortlisted = false;

                // Move candidate to Passed.
                application.Status = "Passed";

                // AIScore is NOT changed.
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"{applications.Count} candidate(s) passed successfully.";


            return RedirectToAction("Management", "HRApplication", new
            {
                status = "review"
            });
        }


        // ============================================================
        // MARK SELECTED REVIEW CANDIDATES AS FAILED
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFailed(
            int vacancyId,
            List<int> selectedIds)
        {
            if (selectedIds == null ||
                selectedIds.Count == 0)
            {
                TempData["Error"] =
                    "Please select at least one candidate.";

                return RedirectToAction(
                    nameof(Review),
                    new { vacancyId });
            }


            var applications =
                await _context.JobApplications
                    .Where(a =>
                        a.JobVacancyId == vacancyId &&
                        selectedIds.Contains(a.Id) &&
                        a.AIScore >= 50 &&
                        a.Status != "Passed" &&
                        a.Status != "Not Shortlisted")
                    .ToListAsync();


            if (applications.Count == 0)
            {
                TempData["Error"] =
                    "No valid review candidates were selected.";

                return RedirectToAction(
                    nameof(Review),
                    new { vacancyId });
            }


            foreach (var application in applications)
            {
                application.HRReviewed = true;

                application.HRShortlisted = false;

                application.Status = "Not Shortlisted";

                // AIScore is NOT changed.
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"{applications.Count} candidate(s) marked as failed.";


            return RedirectToAction(
                nameof(Failed),
                new { vacancyId });
        }


        // ============================================================
        // DELETE APPLICATION
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApplication(
            int id,
            int vacancyId)
        {
            var application =
                await _context.JobApplications
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.JobVacancyId == vacancyId);


            if (application == null)
            {
                return NotFound();
            }


            DeleteFile(application.CVFilePath);
            DeleteFile(application.CoverLetterFilePath);


            _context.JobApplications.Remove(application);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Application deleted permanently.";


            return RedirectToAction(
                nameof(Index),
                new { vacancyId });
        }


        // ============================================================
        // GET AUTOMATIC TOP 15 IDS
        // ============================================================
        //
        // This works on an already-loaded list.
        //
        // TOP 15 PER VACANCY
        //
        // 70+ candidates are eligible.
        // Highest scores are ranked first.
        // Earlier application date breaks ties.
        //
        // ============================================================

        private HashSet<int> GetAutomaticTop15Ids(
            List<JobApplication> applications)
        {
            return applications
                .Where(a =>
                    a.AIScore >= 70 &&
                    a.Status != "Not Shortlisted")
                .GroupBy(a => a.JobVacancyId)
                .SelectMany(g =>
                    g.OrderByDescending(a => a.AIScore)
                     .ThenBy(a => a.AppliedDate)
                     .Take(15))
                .Select(a => a.Id)
                .ToHashSet();
        }


        // ============================================================
        // FILE PATH HELPER
        // ============================================================

        private string? GetFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }


            var relativePath = filePath
                .TrimStart('/')
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);


            var webRootPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath);


            if (System.IO.File.Exists(webRootPath))
            {
                return webRootPath;
            }


            var legacyPath =
                Path.Combine(
                    _environment.ContentRootPath,
                    relativePath);


            if (System.IO.File.Exists(legacyPath))
            {
                return legacyPath;
            }


            return null;
        }


        // ============================================================
        // DELETE FILE HELPER
        // ============================================================

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }


            var fullPath = GetFilePath(filePath);


            if (fullPath != null &&
                System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }


        // ============================================================
        // CONTENT TYPE HELPER
        // ============================================================

        private string GetContentType(string filePath)
        {
            var extension =
                Path.GetExtension(filePath)
                    .ToLowerInvariant();


            return extension switch
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
        }
    }
}