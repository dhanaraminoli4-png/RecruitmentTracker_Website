using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;

        public ApplicationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
        }

        // =========================================================
        // AVAILABLE VACANCIES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Vacancies(
            string? search,
            string? location,
            string? employmentType,
            decimal? minSalary,
            decimal? maxSalary)
        {
            var vacancies = await _context.JobVacancies
                .Where(v => v.IsActive)
                .OrderByDescending(v => v.CreatedDate)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                var searchLower = search.ToLower();

                vacancies = vacancies
                    .Where(v =>
                        (v.JobTitle ?? "").ToLower().Contains(searchLower) ||
                        (v.Description ?? "").ToLower().Contains(searchLower) ||
                        (v.Requirements ?? "").ToLower().Contains(searchLower) ||
                        (v.PreferredRequirements ?? "").ToLower().Contains(searchLower) ||
                        (v.Location ?? "").ToLower().Contains(searchLower) ||
                        (v.EmploymentType ?? "").ToLower().Contains(searchLower))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                location = location.Trim();

                vacancies = vacancies
                    .Where(v =>
                        !string.IsNullOrWhiteSpace(v.Location) &&
                        v.Location.Equals(
                            location,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                employmentType = employmentType.Trim();

                vacancies = vacancies
                    .Where(v =>
                        !string.IsNullOrWhiteSpace(v.EmploymentType) &&
                        v.EmploymentType.Equals(
                            employmentType,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (minSalary.HasValue)
            {
                vacancies = vacancies
                    .Where(v =>
                        v.Salary.HasValue &&
                        v.Salary.Value >= minSalary.Value)
                    .ToList();
            }

            if (maxSalary.HasValue)
            {
                vacancies = vacancies
                    .Where(v =>
                        v.Salary.HasValue &&
                        v.Salary.Value <= maxSalary.Value)
                    .ToList();
            }

            var allActiveVacancies = await _context.JobVacancies
                .Where(v => v.IsActive)
                .ToListAsync();

            var locations = allActiveVacancies
                .Where(v => !string.IsNullOrWhiteSpace(v.Location))
                .Select(v => v.Location!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var employmentTypes = allActiveVacancies
                .Where(v => !string.IsNullOrWhiteSpace(v.EmploymentType))
                .Select(v => v.EmploymentType!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var salaries = allActiveVacancies
                .Where(v => v.Salary.HasValue)
                .Select(v => v.Salary!.Value)
                .ToList();

            decimal salaryMin = 0;
            decimal salaryMax = 500000;

            if (salaries.Any())
            {
                salaryMin =
                    Math.Floor(salaries.Min() / 5000) * 5000;

                salaryMax =
                    Math.Ceiling(salaries.Max() / 5000) * 5000;

                if (salaryMax <= salaryMin)
                {
                    salaryMax = salaryMin + 5000;
                }
            }

            var candidateId =
                _userManager.GetUserId(User);

            var appliedVacancyIds =
                candidateId == null
                    ? new List<int>()
                    : await _context.JobApplications
                        .Where(a =>
                            a.CandidateId == candidateId)
                        .Select(a =>
                            a.JobVacancyId)
                        .ToListAsync();

            ViewBag.AppliedVacancyIds =
                appliedVacancyIds;

            ViewBag.Search =
                search;

            ViewBag.SelectedLocation =
                location;

            ViewBag.SelectedEmploymentType =
                employmentType;

            ViewBag.MinSalary =
                minSalary ?? salaryMin;

            ViewBag.MaxSalary =
                maxSalary ?? salaryMax;

            ViewBag.SalaryMin =
                salaryMin;

            ViewBag.SalaryMax =
                salaryMax;

            ViewBag.Locations =
                locations;

            ViewBag.EmploymentTypes =
                employmentTypes;

            ViewBag.TotalJobs =
                allActiveVacancies.Count;

            return View(vacancies);
        }

        // =========================================================
        // APPLY PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Apply(int vacancyId)
        {
            var vacancy =
                await _context.JobVacancies
                    .FirstOrDefaultAsync(
                        v => v.Id == vacancyId);

            if (vacancy == null)
            {
                TempData["Error"] =
                    "The selected vacancy could not be found.";

                return RedirectToAction(
                    nameof(Vacancies));
            }

            if (!vacancy.IsActive)
            {
                TempData["Error"] =
                    "This vacancy is no longer active.";

                return RedirectToAction(
                    nameof(Vacancies));
            }

            var candidateId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(candidateId))
            {
                return Challenge();
            }

            var alreadyApplied =
                await _context.JobApplications
                    .AnyAsync(a =>
                        a.JobVacancyId == vacancyId &&
                        a.CandidateId == candidateId);

            if (alreadyApplied)
            {
                TempData["Error"] =
                    "You have already applied for this vacancy.";

                return RedirectToAction(
                    nameof(MyApplications));
            }

            ViewBag.Vacancy =
                vacancy;

            return View("Apply");
        }

        // =========================================================
        // SUBMIT APPLICATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            int vacancyId,
            string firstName,
            string lastName,
            string phoneNumber,
            string email,
            IFormFile? cvFile,
            IFormFile? coverLetterFile)
        {
            try
            {
                // =================================================
                // GET VACANCY
                // =================================================

                var vacancy =
                    await _context.JobVacancies
                        .FirstOrDefaultAsync(
                            v => v.Id == vacancyId);

                if (vacancy == null)
                {
                    TempData["Error"] =
                        "The selected vacancy could not be found.";

                    return RedirectToAction(
                        nameof(Vacancies));
                }

                if (!vacancy.IsActive)
                {
                    TempData["Error"] =
                        "This vacancy is no longer active.";

                    return RedirectToAction(
                        nameof(Vacancies));
                }

                // =================================================
                // GET CANDIDATE
                // =================================================

                var candidateId =
                    _userManager.GetUserId(User);

                if (string.IsNullOrEmpty(candidateId))
                {
                    return Unauthorized();
                }

                // =================================================
                // CHECK DUPLICATE APPLICATION
                // =================================================

                var alreadyApplied =
                    await _context.JobApplications
                        .AnyAsync(a =>
                            a.JobVacancyId == vacancyId &&
                            a.CandidateId == candidateId);

                if (alreadyApplied)
                {
                    TempData["Error"] =
                        "You have already applied for this vacancy.";

                    return RedirectToAction(
                        nameof(MyApplications));
                }

                // =================================================
                // CHECK PERSONAL DETAILS
                // =================================================

                if (string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(phoneNumber) ||
                    string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] =
                        "Please complete all required application details.";

                    return RedirectToAction(
                        nameof(Apply),
                        new { vacancyId });
                }

                // =================================================
                // CHECK CV
                // =================================================

                if (cvFile == null ||
                    cvFile.Length == 0)
                {
                    TempData["Error"] =
                        "Please upload your CV.";

                    return RedirectToAction(
                        nameof(Apply),
                        new { vacancyId });
                }

                // =================================================
                // CV FILE TYPE
                // =================================================

                var cvExtension =
                    Path.GetExtension(
                        cvFile.FileName)
                        .ToLowerInvariant();

                if (cvExtension != ".pdf" &&
                    cvExtension != ".doc" &&
                    cvExtension != ".docx")
                {
                    TempData["Error"] =
                        "Only PDF, DOC and DOCX files are allowed.";

                    return RedirectToAction(
                        nameof(Apply),
                        new { vacancyId });
                }

                // =================================================
                // CV SIZE
                // =================================================

                if (cvFile.Length >
                    5 * 1024 * 1024)
                {
                    TempData["Error"] =
                        "Your CV must be smaller than 5 MB.";

                    return RedirectToAction(
                        nameof(Apply),
                        new { vacancyId });
                }

                // =================================================
                // REQUIRED COVER LETTER
                // =================================================

                if (vacancy.RequiresCoverLetter &&
                    (coverLetterFile == null ||
                     coverLetterFile.Length == 0))
                {
                    TempData["Error"] =
                        "A cover letter is required for this vacancy.";

                    return RedirectToAction(
                        nameof(Apply),
                        new { vacancyId });
                }

                // =================================================
                // COVER LETTER VALIDATION
                // =================================================

                if (coverLetterFile != null &&
                    coverLetterFile.Length > 0)
                {
                    var coverExtension =
                        Path.GetExtension(
                            coverLetterFile.FileName)
                            .ToLowerInvariant();

                    if (coverExtension != ".pdf" &&
                        coverExtension != ".doc" &&
                        coverExtension != ".docx")
                    {
                        TempData["Error"] =
                            "Cover letter must be PDF, DOC or DOCX.";

                        return RedirectToAction(
                            nameof(Apply),
                            new { vacancyId });
                    }

                    if (coverLetterFile.Length >
                        5 * 1024 * 1024)
                    {
                        TempData["Error"] =
                            "Your cover letter must be smaller than 5 MB.";

                        return RedirectToAction(
                            nameof(Apply),
                            new { vacancyId });
                    }
                }

                // =================================================
                // CREATE UPLOAD FOLDER
                // =================================================

                var uploadFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "UploadedCVs");

                Directory.CreateDirectory(
                    uploadFolder);

                // =================================================
                // SAVE CV
                // =================================================

                var cvFileName =
                    Guid.NewGuid().ToString("N") +
                    cvExtension;

                var cvFilePath =
                    Path.Combine(
                        uploadFolder,
                        cvFileName);

                using (var stream =
                    new FileStream(
                        cvFilePath,
                        FileMode.Create))
                {
                    await cvFile.CopyToAsync(stream);
                }

                // =================================================
                // SAVE COVER LETTER
                // =================================================

                string? coverLetterPath = null;

                if (coverLetterFile != null &&
                    coverLetterFile.Length > 0)
                {
                    var coverExtension =
                        Path.GetExtension(
                            coverLetterFile.FileName)
                            .ToLowerInvariant();

                    var coverFileName =
                        Guid.NewGuid().ToString("N") +
                        coverExtension;

                    var coverFilePath =
                        Path.Combine(
                            uploadFolder,
                            coverFileName);

                    using (var stream =
                        new FileStream(
                            coverFilePath,
                            FileMode.Create))
                    {
                        await coverLetterFile.CopyToAsync(
                            stream);
                    }

                    coverLetterPath =
                        "/UploadedCVs/" +
                        coverFileName;
                }

                // =================================================
                // CREATE APPLICATION
                // =================================================

                var application =
                    new JobApplication
                    {
                        FirstName =
                            firstName.Trim(),

                        LastName =
                            lastName.Trim(),

                        CandidateId =
                            candidateId,

                        JobVacancyId =
                            vacancyId,

                        Status =
                            "Applied",

                        CVFilePath =
                            "/UploadedCVs/" +
                            cvFileName,

                        CoverLetterFilePath =
                            coverLetterPath,

                        AIScore = 0,

                        AIRequiredScore = 0,

                        AIPreferredScore = 0,

                        AIOverallScore = 0,

                        AIRecommendation =
                            "Not Analyzed",

                        AIRequiredResults =
                            "[]",

                        AIPreferredResults =
                            "[]",

                        AIRank = 0,

                        HRReviewed = false,

                        HRShortlisted = false,

                        AppliedDate =
                            DateTime.Now
                    };

                // =================================================
                // SAVE APPLICATION
                // =================================================

                _context.JobApplications.Add(
                    application);

                await _context.SaveChangesAsync();

                // =================================================
                // AI ANALYSIS
                // =================================================

                var aiResult =
                    await AnalyzeCVWithAI(
                        cvFilePath,
                        vacancy);

                // =================================================
                // SAVE AI RESULT
                // =================================================

                if (aiResult != null &&
                    aiResult.Success)
                {
                    application.AIScore =
                        aiResult.FinalScore;

                    application.AIRequiredScore =
                        aiResult.RequiredScore;

                    application.AIPreferredScore =
                        aiResult.PreferredScore;

                    application.AIOverallScore =
                        aiResult.OverallScore;

                    application.AIRecommendation =
                        aiResult.Recommendation;

                    application.AIRequiredResults =
                        JsonSerializer.Serialize(
                            aiResult.RequiredResults);

                    application.AIPreferredResults =
                        JsonSerializer.Serialize(
                            aiResult.PreferredResults);
                }
                else
                {
                    application.AIRecommendation =
                        "AI Analysis Failed";
                }

                // =================================================
                // APPLICATION STATUS
                // =================================================

                application.Status =
                    "Under Review";

                await _context.SaveChangesAsync();

                // =================================================
                // SUCCESS
                // =================================================

                TempData["Success"] =
                    "Your application was submitted successfully.";

                return RedirectToAction(
                    nameof(MyApplications));
            }
            catch (Exception ex)
            {
                return Content(
                    "APPLICATION ERROR\n\n" +
                    ex.GetType().FullName +
                    "\n\n" +
                    ex.Message +
                    "\n\n" +
                    ex.StackTrace);
            }
        }

        // =========================================================
        // AI ANALYSIS
        // =========================================================

        private async Task<AIResult?> AnalyzeCVWithAI(
            string filePath,
            JobVacancy vacancy)
        {
            try
            {
                var client =
                    _httpClientFactory.CreateClient();

                using var form =
                    new MultipartFormDataContent();

                await using var fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read);

                var fileContent =
                    new StreamContent(fileStream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        GetContentType(filePath));

                form.Add(
                    fileContent,
                    "cv",
                    Path.GetFileName(filePath));

                // =================================================
                // JOB INFORMATION
                // =================================================

                form.Add(
                    new StringContent(
                        vacancy.JobTitle ?? ""),
                    "job_title");

                form.Add(
                    new StringContent(
                        vacancy.Description ?? ""),
                    "job_description");

                // =================================================
                // REQUIRED REQUIREMENTS
                // =================================================

                var requiredRequirements =
                    ParseRequirements(
                        vacancy.Requirements);

                // =================================================
                // PREFERRED REQUIREMENTS
                // =================================================

                var preferredRequirements =
                    ParseRequirements(
                        vacancy.PreferredRequirements);

                // =================================================
                // SEND REQUIRED REQUIREMENTS
                // =================================================

                form.Add(
                    new StringContent(
                        string.Join(
                            "|",
                            requiredRequirements)),
                    "required_requirements");

                // =================================================
                // SEND PREFERRED REQUIREMENTS
                // =================================================

                form.Add(
                    new StringContent(
                        string.Join(
                            "|",
                            preferredRequirements)),
                    "preferred_requirements");

                // =================================================
                // CALL PYTHON AI API
                // =================================================

                var response =
                    await client.PostAsync(
                        "http://127.0.0.1:5000/analyze",
                        form);

                // =================================================
                // READ RESPONSE
                // =================================================

                var json =
                    await response.Content
                        .ReadAsStringAsync();

                // =================================================
                // CHECK HTTP STATUS
                // =================================================

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "AI API returned HTTP " +
                        (int)response.StatusCode +
                        "\n\n" +
                        "Response from Python API:\n" +
                        json);
                }

                // =================================================
                // DESERIALIZE RESULT
                // =================================================

                var result =
                    JsonSerializer.Deserialize<AIResult>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result == null)
                {
                    throw new Exception(
                        "AI API returned an empty or invalid response.");
                }

                // =================================================
                // CHECK AI RESULT
                // =================================================

                if (!result.Success)
                {
                    throw new Exception(
                        "AI API reported an error:\n" +
                        result.Error);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "AI CONNECTION ERROR:\n\n" +
                    ex.Message,
                    ex);
            }
        }

        // =========================================================
        // REQUIREMENT PARSER
        // =========================================================

        private List<string> ParseRequirements(
            string? requirements)
        {
            var result =
                new List<string>();

            if (string.IsNullOrWhiteSpace(
                requirements))
            {
                return result;
            }

            var lines =
                requirements.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var cleaned =
                    line
                        .Trim()
                        .TrimStart(
                            '-',
                            '*',
                            '•')
                        .Trim();

                if (!string.IsNullOrWhiteSpace(
                    cleaned))
                {
                    result.Add(cleaned);
                }
            }

            return result;
        }

        // =========================================================
        // CONTENT TYPE
        // =========================================================

        private string GetContentType(
            string filePath)
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

        // =========================================================
        // MY APPLICATIONS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var candidateId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(candidateId))
            {
                return Unauthorized();
            }

            var applications =
                await _context.JobApplications
                    .Include(a => a.JobVacancy)
                    .Where(a =>
                        a.CandidateId == candidateId)
                    .OrderByDescending(
                        a => a.AppliedDate)
                    .ToListAsync();

            return View(applications);
        }
    }

    // =============================================================
    // AI RESULT
    // =============================================================

    public class AIResult
    {
        public bool Success { get; set; }

        public double OverallScore { get; set; }

        public double RequiredScore { get; set; }

        public double PreferredScore { get; set; }

        public double FinalScore { get; set; }

        public string Recommendation { get; set; }
            = "Not Analyzed";

        public string Error { get; set; }
            = "";

        [JsonPropertyName("requiredResults")]
        public List<AIRequirementResult> RequiredResults { get; set; }
            = new();

        [JsonPropertyName("preferredResults")]
        public List<AIRequirementResult> PreferredResults { get; set; }
            = new();
    }

    // =============================================================
    // AI REQUIREMENT RESULT
    // =============================================================

    public class AIRequirementResult
    {
        public string Requirement { get; set; }
            = "";

        public double Score { get; set; }

        public string Status { get; set; }
            = "";

        public string Method { get; set; }
            = "";
    }
}