using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.Json;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HiringManager")]
    public class HiringManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HiringManagerController(
    ApplicationDbContext context,
    IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==========================================================
        // AI ANALYSIS
        // ==========================================================
        public async Task<IActionResult> AIAnalysis(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

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

            // Reuse HR's existing AIAnalysis view.
            return View(
                "~/Views/HRApplication/AIAnalysis.cshtml",
                model);
        }
        // ==========================================================
        // APPLICATIONS SENT TO HIRING MANAGER
        // ==========================================================
        // ==========================================
        // APPLICATIONS SENT TO HIRING MANAGER
        // FILTERED BY JOB VACANCY
        // ==========================================
        public async Task<IActionResult> Applications(
            int? jobId = null,
            string status = "all")
        {
            // ==========================================
            // 1. GET JOBS DIRECTLY FROM JOB VACANCIES
            // ==========================================
            var jobs = await _context.JobVacancies
                .OrderByDescending(v => v.IsActive)
                .ThenBy(v => v.JobTitle)
                .ToListAsync();

            // Send jobs to the view for the dropdown
            ViewBag.Jobs = jobs;

            // Remember selected job
            ViewBag.SelectedJobId = jobId;

            // Remember selected status
            status = string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.ToLowerInvariant();

            ViewBag.SelectedStatus = status;


            // ==========================================
            // 2. ONLY APPLICATIONS HR SENT TO HM
            // ==========================================
            var query = _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .Where(a => a.HRShortlisted)
                .AsQueryable();


            // ==========================================
            // 3. FILTER BY SELECTED JOB
            // ==========================================
            if (jobId.HasValue)
            {
                query = query.Where(a =>
                    a.JobVacancyId == jobId.Value);
            }


            // ==========================================
            // 4. FILTER BY HM DECISION
            // ==========================================
            switch (status)
            {
                case "pending":

                    query = query.Where(a =>
                        !a.HiringManagerReviewed);

                    break;


                case "approved":

                    query = query.Where(a =>
                        a.HiringManagerReviewed &&
                        a.HiringManagerDecision ==
                        "ApprovedForInterview");

                    break;


                case "rejected":

                    query = query.Where(a =>
                        a.HiringManagerReviewed &&
                        a.HiringManagerDecision ==
                        "Rejected");

                    break;
            }


            // ==========================================
            // 5. LOAD CANDIDATES
            // ==========================================
            var applications = await query
                .OrderBy(a => a.JobVacancy!.JobTitle)
                .ThenBy(a => a.HiringManagerReviewed)
                .ThenByDescending(a => a.AIScore)
                .ThenBy(a => a.AppliedDate)
                .ToListAsync();


            // ==========================================
            // 6. SELECTED JOB INFORMATION
            // ==========================================
            if (jobId.HasValue)
            {
                var selectedJob = jobs
                    .FirstOrDefault(v => v.Id == jobId.Value);

                ViewBag.SelectedJobTitle =
                    selectedJob?.JobTitle ?? "Unknown Job";

                ViewBag.SelectedJobCode =
                    selectedJob?.JobCode ?? "";
            }
            else
            {
                ViewBag.SelectedJobTitle = "All Jobs";
                ViewBag.SelectedJobCode = "";
            }


            return View(applications);
        }

        // ==========================================================
        // VIEW ONE APPLICATION
        // ==========================================================
        public async Task<IActionResult> Review(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a => a.JobVacancy)
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // ==========================================================
        // APPROVE ONE CANDIDATE
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveForInterview(
            int id,
            string? comments)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

            if (application == null)
            {
                return NotFound();
            }

            application.HiringManagerReviewed = true;
            application.HiringManagerShortlisted = true;
            application.HiringManagerDecision =
                "ApprovedForInterview";
            application.HiringManagerComments =
                comments ?? "";

            // Keep candidate-facing status simple.
            application.Status = "Shortlisted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Candidate approved for interview.";

            return RedirectToAction(
                nameof(Review),
                new { id });
        }

        // ==========================================================
        // REJECT ONE CANDIDATE
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? comments)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

            if (application == null)
            {
                return NotFound();
            }

            application.HiringManagerReviewed = true;
            application.HiringManagerShortlisted = false;
            application.HiringManagerDecision = "Rejected";
            application.HiringManagerComments =
                comments ?? "";

            // Internal HM rejection does not change
            // candidate-facing Sprint 1 status.
            application.Status = "Shortlisted";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Candidate rejected by Hiring Manager.";

            return RedirectToAction(
                nameof(Review),
                new { id });
        }

        // ==========================================================
        // BULK APPROVE FOR INTERVIEW
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(
            List<int> selectedIds,
            int jobId)
        {
            if (selectedIds == null ||
                selectedIds.Count == 0)
            {
                TempData["Error"] =
                    "Please select at least one candidate.";

                return RedirectToAction(
                    nameof(Applications),
                    new { jobId });
            }

            // IMPORTANT:
            // JobVacancyId check prevents candidates from
            // different jobs being processed together.
            var applications = await _context.JobApplications
                .Where(a =>
                    selectedIds.Contains(a.Id) &&
                    a.HRShortlisted &&
                    a.JobVacancyId == jobId)
                .ToListAsync();

            if (applications.Count == 0)
            {
                TempData["Error"] =
                    "No valid candidates were selected.";

                return RedirectToAction(
                    nameof(Applications),
                    new { jobId });
            }

            foreach (var application in applications)
            {
                application.HiringManagerReviewed = true;
                application.HiringManagerShortlisted = true;
                application.HiringManagerDecision =
                    "ApprovedForInterview";

                application.HiringManagerComments = "";

                application.Status = "Shortlisted";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{applications.Count} candidate(s) approved for interview.";

            return RedirectToAction(
                nameof(Applications),
                new { jobId });
        }

        // ==========================================================
        // BULK REJECT
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReject(
            List<int> selectedIds,
            int jobId)
        {
            if (selectedIds == null ||
                selectedIds.Count == 0)
            {
                TempData["Error"] =
                    "Please select at least one candidate.";

                return RedirectToAction(
                    nameof(Applications),
                    new { jobId });
            }

            var applications = await _context.JobApplications
                .Where(a =>
                    selectedIds.Contains(a.Id) &&
                    a.HRShortlisted &&
                    a.JobVacancyId == jobId)
                .ToListAsync();

            if (applications.Count == 0)
            {
                TempData["Error"] =
                    "No valid candidates were selected.";

                return RedirectToAction(
                    nameof(Applications),
                    new { jobId });
            }

            foreach (var application in applications)
            {
                application.HiringManagerReviewed = true;
                application.HiringManagerShortlisted = false;
                application.HiringManagerDecision = "Rejected";
                application.HiringManagerComments = "";

                application.Status = "Shortlisted";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{applications.Count} candidate(s) rejected.";

            return RedirectToAction(
                nameof(Applications),
                new { jobId });
        }

        // ==========================================================
        // INTERVIEW REVIEWS
        // FILTER BY JOB VACANCY
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> InterviewReviews(
            int? jobId = null)
        {
            // ==========================================
            // 1. GET JOB VACANCIES
            // ==========================================
            var jobs = await _context.JobVacancies
                .OrderByDescending(v => v.IsActive)
                .ThenBy(v => v.JobTitle)
                .ToListAsync();

            ViewBag.Jobs = jobs;
            ViewBag.SelectedJobId = jobId;


            // ==========================================
            // 2. BASE INTERVIEW QUERY
            // ==========================================
            var query = _context.Interviews

                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.Candidate)

                .Include(i => i.JobApplication)
                    .ThenInclude(a => a.JobVacancy)

                .Include(i => i.InterviewRound)

                .Include(i => i.InterviewType)

                .Include(i => i.Assessments)
                    .ThenInclude(a => a.AssessmentTemplate)

                .Include(i => i.Feedbacks)
                    .ThenInclude(f => f.Interviewer)

                .AsSplitQuery()

                .AsQueryable();


            // ==========================================
            // 3. FILTER BY JOB VACANCY
            // ==========================================
            if (jobId.HasValue)
            {
                query = query.Where(i =>
                    i.JobApplication != null &&
                    i.JobApplication.JobVacancyId == jobId.Value);
            }


            // ==========================================
            // 4. LOAD INTERVIEWS
            // ==========================================
            var interviews = await query
                .OrderByDescending(i => i.InterviewDate)
                .ThenByDescending(i => i.InterviewTime)
                .ToListAsync();


            // ==========================================
            // 5. SELECTED JOB INFORMATION
            // ==========================================
            if (jobId.HasValue)
            {
                var selectedJob = jobs
                    .FirstOrDefault(v => v.Id == jobId.Value);

                ViewBag.SelectedJobTitle =
                    selectedJob?.JobTitle ?? "Unknown Job";

                ViewBag.SelectedJobCode =
                    selectedJob?.JobCode ?? "";
            }
            else
            {
                ViewBag.SelectedJobTitle =
                    "All Job Vacancies";

                ViewBag.SelectedJobCode =
                    "";
            }


            return View(interviews);
        }

        // ==========================================================
        // REVIEW ONE INTERVIEW
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> InterviewReview(int id)
        {
            var interview =
                await _context.Interviews

                    .Include(i => i.JobApplication)
                        .ThenInclude(a => a.Candidate)

                    .Include(i => i.JobApplication)
                        .ThenInclude(a => a.JobVacancy)

                    .Include(i => i.InterviewRound)

                    .Include(i => i.InterviewType)

                    .Include(i => i.Assessments)
                        .ThenInclude(a => a.AssessmentTemplate)
                            .ThenInclude(t => t.Questions)

                    .Include(i => i.Assessments)
                        .ThenInclude(a => a.Answers)
                            .ThenInclude(a => a.AssessmentQuestion)

                    .Include(i => i.Assessments)
                        .ThenInclude(a => a.ConductedByInterviewer)

                    .Include(i => i.Feedbacks)
                        .ThenInclude(f => f.Interviewer)

                    .AsSplitQuery()

                    .FirstOrDefaultAsync(i =>
                        i.Id == id);

            if (interview == null)
            {
                return NotFound();
            }

            return View(interview);
        }

        // ==========================================================
        // VIEW CV
        // ==========================================================
        public async Task<IActionResult> ViewCV(int id)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

            if (application == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(application.CVFilePath))
            {
                return NotFound("CV file was not found.");
            }

            var fullPath = GetFilePath(application.CVFilePath);

            if (fullPath == null)
            {
                return NotFound("CV file does not exist.");
            }

            return PhysicalFile(
                fullPath,
                GetContentType(fullPath));
        }

        // ==========================================================
        // DECIDE INTERVIEW ROUND
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideRound(
            int interviewId,
            string decision,
            string? notes)
        {
            if (decision != "Pass" &&
                decision != "Fail")
            {
                TempData["Error"] =
                    "Invalid round decision.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        id = interviewId
                    }
                );
            }


            var interview =
                await _context.Interviews

                    .Include(i => i.JobApplication)

                    .Include(i => i.InterviewRound)

                    .FirstOrDefaultAsync(i =>
                        i.Id == interviewId);


            if (interview == null)
            {
                return NotFound();
            }


            // Interview must be completed first
            if (interview.Status != "Completed")
            {
                TempData["Error"] =
                    "The interview must be completed before a round decision can be made.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        id = interviewId
                    }
                );
            }


            // Prevent duplicate decisions
            if (interview.RoundResult != "Pending")
            {
                TempData["Error"] =
                    "A decision has already been recorded for this interview round.";

                return RedirectToAction(
                    nameof(InterviewReview),
                    new
                    {
                        id = interviewId
                    }
                );
            }


            var currentUserName =
                User?.Identity?.Name
                ?? "Hiring Manager";


            if (decision == "Pass")
            {
                interview.RoundResult =
                    "Passed";

                if (interview.JobApplication != null)
                {
                    interview.JobApplication.InterviewStageStatus =
                        "In Progress";
                }
            }
            else
            {
                interview.RoundResult =
                    "Failed";

                if (interview.JobApplication != null)
                {
                    interview.JobApplication.InterviewStageStatus =
                        "Failed";

                    interview.JobApplication.InterviewProcessCompleted =
                        true;
                }
            }


            interview.RoundDecisionNotes =
                string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim();

            interview.RoundDecisionDate =
                DateTime.Now;

            interview.RoundDecisionBy =
                currentUserName;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                decision == "Pass"
                    ? "Interview round passed successfully."
                    : "Interview round failed successfully.";


            return RedirectToAction(
                nameof(InterviewReview),
                new
                {
                    id = interviewId
                }
            );
        }


        // ==========================================================
        // VIEW COVER LETTER
        // ==========================================================
        public async Task<IActionResult> ViewCoverLetter(int id)
        {
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.HRShortlisted);

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

            var fullPath =
                GetFilePath(application.CoverLetterFilePath);

            if (fullPath == null)
            {
                return NotFound(
                    "Cover letter file does not exist.");
            }

            return PhysicalFile(
                fullPath,
                GetContentType(fullPath));
        }


        // ==========================================================
        // FILE PATH
        // ==========================================================
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

            var webRootPath = Path.Combine(
                _environment.WebRootPath,
                relativePath);

            if (System.IO.File.Exists(webRootPath))
            {
                return webRootPath;
            }

            var legacyPath = Path.Combine(
                _environment.ContentRootPath,
                relativePath);

            if (System.IO.File.Exists(legacyPath))
            {
                return legacyPath;
            }

            return null;
        }


        // ==========================================================
        // CONTENT TYPE
        // ==========================================================
        private string GetContentType(string filePath)
        {
            var extension =
                Path.GetExtension(filePath)
                    .ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",

                ".doc" => "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                _ => "application/octet-stream"
            };
        }
    }
}