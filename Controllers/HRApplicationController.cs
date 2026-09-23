using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        // ============================================================
        // RECRUITMENT REPORTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Reports(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? vacancyId = null)
        {
            // ========================================================
            // APPLICATION QUERY
            // ========================================================

            var applicationQuery =
                _context.JobApplications

                    .Include(a =>
                        a.JobVacancy)

                    .AsNoTracking()

                    .AsQueryable();


            if (fromDate.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.AppliedDate.Date >=
                        fromDate.Value.Date);
            }


            if (toDate.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.AppliedDate.Date <=
                        toDate.Value.Date);
            }


            if (vacancyId.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.JobVacancyId ==
                        vacancyId.Value);
            }


            var applicationList =
                await applicationQuery
                    .ToListAsync();



            // ========================================================
            // VACANCIES FOR FILTER
            // ========================================================

            var vacancies =
                await _context.JobVacancies

                    .OrderByDescending(v =>
                        v.IsActive)

                    .ThenBy(v =>
                        v.JobTitle)

                    .AsNoTracking()

                    .ToListAsync();


            ViewBag.Vacancies =
                vacancies;

            ViewBag.SelectedVacancyId =
                vacancyId;



            if (vacancyId.HasValue)
            {
                var selectedVacancy =
                    vacancies.FirstOrDefault(v =>
                        v.Id == vacancyId.Value);


                ViewBag.SelectedVacancyTitle =
                    selectedVacancy?.JobTitle
                    ?? "Unknown Vacancy";
            }
            else
            {
                ViewBag.SelectedVacancyTitle =
                    "All Vacancies";
            }



            // ========================================================
            // AUTOMATIC AI PASSED
            // TOP 15 PER VACANCY
            // ========================================================

            var automaticPassedIds =
                applicationList

                    .GroupBy(a =>
                        a.JobVacancyId)

                    .SelectMany(group =>
                        group

                            .Where(a =>
                                a.AIScore >= 70 &&
                                a.Status !=
                                "Not Shortlisted")

                            .OrderByDescending(a =>
                                a.AIScore)

                            .ThenBy(a =>
                                a.AppliedDate)

                            .Take(15)

                            .Select(a =>
                                a.Id))

                    .ToHashSet();



            // ========================================================
            // MAIN APPLICATION COUNTS
            // ========================================================

            ViewBag.TotalApplications =
                applicationList.Count;


            ViewBag.Passed =
                applicationList.Count(a =>
                    automaticPassedIds.Contains(a.Id) ||
                    (
                        a.Status == "Passed" &&
                        a.Status != "Not Shortlisted"
                    ));


            ViewBag.HRReviewed =
                applicationList.Count(a =>
                    a.HRReviewed);


            ViewBag.Shortlisted =
                applicationList.Count(a =>
                    a.HRShortlisted);


            ViewBag.NotShortlisted =
                applicationList.Count(a =>
                    a.Status == "Not Shortlisted");



            // ========================================================
            // HIRING MANAGER COUNTS
            // ========================================================

            ViewBag.HMApproved =
                applicationList.Count(a =>
                    a.HiringManagerReviewed &&
                    a.HiringManagerDecision ==
                    "ApprovedForInterview");


            ViewBag.HMRejected =
                applicationList.Count(a =>
                    a.HiringManagerReviewed &&
                    a.HiringManagerDecision ==
                    "Rejected");


            ViewBag.HMPending =
                applicationList.Count(a =>
                    a.HRShortlisted &&
                    !a.HiringManagerReviewed);



            // ========================================================
            // APPLICATION IDS IN CURRENT REPORT
            // ========================================================

            var applicationIds =
                applicationList

                    .Select(a =>
                        a.Id)

                    .ToList();



            // ========================================================
            // INTERVIEW QUERY
            // ========================================================

            var interviewQuery =
                _context.Interviews

                    .Include(i =>
                        i.JobApplication)

                    .Include(i =>
                        i.Feedbacks)

                    .AsNoTracking()

                    .Where(i =>
                        applicationIds.Contains(
                            i.JobApplicationId));



            var interviews =
                await interviewQuery
                    .ToListAsync();



            // ========================================================
            // INTERVIEW COUNTS
            // ========================================================

            ViewBag.TotalInterviews =
                interviews.Count;


            ViewBag.CompletedInterviews =
                interviews.Count(i =>
                    i.Status == "Completed");


            ViewBag.ScheduledInterviews =
                interviews.Count(i =>
                    i.Status == "Scheduled");


            ViewBag.InProgressInterviews =
                interviews.Count(i =>
                    i.Status == "In Progress");


            ViewBag.PendingInterviewDecision =
                interviews.Count(i =>
                    i.Status == "Completed" &&
                    (
                        string.IsNullOrWhiteSpace(
                            i.RoundResult) ||
                        i.RoundResult == "Pending"
                    ));


            ViewBag.PassedRounds =
                interviews.Count(i =>
                    i.RoundResult == "Passed");


            ViewBag.FailedRounds =
                interviews.Count(i =>
                    i.RoundResult == "Failed");



            // ========================================================
            // FEEDBACK COUNTS
            // ========================================================

            ViewBag.FeedbackSubmitted =
                interviews.Count(i =>
                    i.FeedbackStatus ==
                    "Submitted");


            ViewBag.FeedbackPending =
                interviews.Count(i =>
                    i.Status != "Completed" &&
                    i.FeedbackStatus !=
                    "Submitted");



            // ========================================================
            // VACANCY PERFORMANCE TABLE
            // ========================================================

            var rows =
                applicationList

                    .GroupBy(a =>
                        new
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
                        var vacancyApplications =
                            g.ToList();


                        var ids =
                            vacancyApplications

                                .Select(a =>
                                    a.Id)

                                .ToHashSet();


                        var vacancyInterviews =
                            interviews

                                .Where(i =>
                                    ids.Contains(
                                        i.JobApplicationId))

                                .ToList();


                        return new
                        {
                            g.Key.JobVacancyId,

                            g.Key.JobTitle,

                            g.Key.Location,

                            g.Key.CreatedDate,

                            g.Key.ClosingDate,

                            g.Key.IsActive,


                            Applications =
                                vacancyApplications.Count,


                            Passed =
                                vacancyApplications.Count(a =>
                                    automaticPassedIds.Contains(
                                        a.Id) ||
                                    a.Status == "Passed"),


                            HRReviewed =
                                vacancyApplications.Count(a =>
                                    a.HRReviewed),


                            Shortlisted =
                                vacancyApplications.Count(a =>
                                    a.HRShortlisted),


                            HMApproved =
                                vacancyApplications.Count(a =>
                                    a.HiringManagerDecision ==
                                    "ApprovedForInterview"),


                            HMRejected =
                                vacancyApplications.Count(a =>
                                    a.HiringManagerDecision ==
                                    "Rejected"),


                            Interviews =
                                vacancyInterviews.Count,


                            CompletedInterviews =
                                vacancyInterviews.Count(i =>
                                    i.Status ==
                                    "Completed"),


                            PassedRounds =
                                vacancyInterviews.Count(i =>
                                    i.RoundResult ==
                                    "Passed"),


                            FailedRounds =
                                vacancyInterviews.Count(i =>
                                    i.RoundResult ==
                                    "Failed")
                        };
                    })

                    .OrderByDescending(x =>
                        x.CreatedDate)

                    .ToList();



            // ========================================================
            // FUNNEL PERCENTAGES
            // ========================================================

            var total =
                applicationList.Count;


            ViewBag.HRReviewedPercent =
                total == 0
                    ? 0
                    : Math.Round(
                        (double)ViewBag.HRReviewed /
                        total * 100,
                        1);


            ViewBag.ShortlistedPercent =
                total == 0
                    ? 0
                    : Math.Round(
                        (double)ViewBag.Shortlisted /
                        total * 100,
                        1);


            ViewBag.HMApprovedPercent =
                total == 0
                    ? 0
                    : Math.Round(
                        (double)ViewBag.HMApproved /
                        total * 100,
                        1);


            ViewBag.InterviewPercent =
                total == 0
                    ? 0
                    : Math.Round(
                        (double)ViewBag.TotalInterviews /
                        total * 100,
                        1);



            // ========================================================
            // FILTER VALUES
            // ========================================================

            ViewBag.FromDate =
                fromDate?
                    .ToString("yyyy-MM-dd");


            ViewBag.ToDate =
                toDate?
                    .ToString("yyyy-MM-dd");


            return View(rows);
        }

        // ============================================================
        // EXPORT RECRUITMENT REPORT AS PDF
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ExportReportsPdf(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? vacancyId = null)
        {
            // ========================================================
            // APPLICATION QUERY
            // ========================================================

            var applicationQuery =
                _context.JobApplications
                    .Include(a => a.JobVacancy)
                    .AsNoTracking()
                    .AsQueryable();


            if (fromDate.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.AppliedDate.Date >=
                        fromDate.Value.Date);
            }


            if (toDate.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.AppliedDate.Date <=
                        toDate.Value.Date);
            }


            if (vacancyId.HasValue)
            {
                applicationQuery =
                    applicationQuery.Where(a =>
                        a.JobVacancyId ==
                        vacancyId.Value);
            }


            var applications =
                await applicationQuery
                    .ToListAsync();


            // ========================================================
            // SELECTED VACANCY
            // ========================================================

            var reportVacancyName =
                "All Vacancies";


            if (vacancyId.HasValue)
            {
                var vacancy =
                    await _context.JobVacancies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v =>
                            v.Id == vacancyId.Value);


                reportVacancyName =
                    vacancy?.JobTitle
                    ?? "Unknown Vacancy";
            }


            // ========================================================
            // AI PASSED - TOP 15 PER VACANCY
            // ========================================================

            var automaticPassedIds =
                applications
                    .GroupBy(a =>
                        a.JobVacancyId)
                    .SelectMany(group =>
                        group
                            .Where(a =>
                                a.AIScore >= 70 &&
                                a.Status !=
                                "Not Shortlisted")
                            .OrderByDescending(a =>
                                a.AIScore)
                            .ThenBy(a =>
                                a.AppliedDate)
                            .Take(15)
                            .Select(a =>
                                a.Id))
                    .ToHashSet();


            // ========================================================
            // APPLICATION COUNTS
            // ========================================================

            var totalApplications =
                applications.Count;


            var aiPassed =
                applications.Count(a =>
                    automaticPassedIds.Contains(a.Id) ||
                    (
                        a.Status == "Passed" &&
                        a.Status != "Not Shortlisted"
                    ));


            var hrReviewed =
                applications.Count(a =>
                    a.HRReviewed);


            var shortlisted =
                applications.Count(a =>
                    a.HRShortlisted);


            var hmApproved =
                applications.Count(a =>
                    a.HiringManagerReviewed &&
                    a.HiringManagerDecision ==
                    "ApprovedForInterview");


            var hmRejected =
                applications.Count(a =>
                    a.HiringManagerReviewed &&
                    a.HiringManagerDecision ==
                    "Rejected");


            var hmPending =
                applications.Count(a =>
                    a.HRShortlisted &&
                    !a.HiringManagerReviewed);


            // ========================================================
            // INTERVIEWS
            // ========================================================

            var applicationIds =
                applications
                    .Select(a => a.Id)
                    .ToList();


            var interviews =
                await _context.Interviews
                    .Where(i =>
                        applicationIds.Contains(
                            i.JobApplicationId))
                    .AsNoTracking()
                    .ToListAsync();


            var totalInterviews =
                interviews.Count;


            var completedInterviews =
                interviews.Count(i =>
                    i.Status == "Completed");


            var pendingDecisions =
                interviews.Count(i =>
                    i.Status == "Completed" &&
                    (
                        string.IsNullOrWhiteSpace(
                            i.RoundResult) ||
                        i.RoundResult == "Pending"
                    ));


            var passedRounds =
                interviews.Count(i =>
                    i.RoundResult == "Passed");


            var failedRounds =
                interviews.Count(i =>
                    i.RoundResult == "Failed");


            // ========================================================
            // VACANCY TABLE
            // ========================================================

            var vacancyRows =
                applications
                    .GroupBy(a => new
                    {
                        a.JobVacancyId,

                        JobTitle =
                            a.JobVacancy != null
                                ? a.JobVacancy.JobTitle
                                : "Unknown Vacancy",

                        Location =
                            a.JobVacancy != null
                                ? a.JobVacancy.Location
                                : "",

                        IsActive =
                            a.JobVacancy != null &&
                            a.JobVacancy.IsActive
                    })
                    .Select(g =>
                    {
                        var vacancyApplications =
                            g.ToList();


                        var ids =
                            vacancyApplications
                                .Select(a => a.Id)
                                .ToHashSet();


                        var vacancyInterviews =
                            interviews
                                .Where(i =>
                                    ids.Contains(
                                        i.JobApplicationId))
                                .ToList();


                        return new
                        {
                            g.Key.JobTitle,
                            g.Key.Location,
                            g.Key.IsActive,

                            Applications =
                                vacancyApplications.Count,

                            AIPassed =
                                vacancyApplications.Count(a =>
                                    automaticPassedIds.Contains(
                                        a.Id) ||
                                    (
                                        a.Status == "Passed" &&
                                        a.Status !=
                                        "Not Shortlisted"
                                    )),

                            HRReviewed =
                                vacancyApplications.Count(a =>
                                    a.HRReviewed),

                            Shortlisted =
                                vacancyApplications.Count(a =>
                                    a.HRShortlisted),

                            HMApproved =
                                vacancyApplications.Count(a =>
                                    a.HiringManagerDecision ==
                                    "ApprovedForInterview"),

                            Interviews =
                                vacancyInterviews.Count,

                            PassedRounds =
                                vacancyInterviews.Count(i =>
                                    i.RoundResult ==
                                    "Passed"),

                            FailedRounds =
                                vacancyInterviews.Count(i =>
                                    i.RoundResult ==
                                    "Failed")
                        };
                    })
                    .OrderByDescending(x =>
                        x.Applications)
                    .ToList();


            // ========================================================
            // REPORT LABELS
            // ========================================================

            var periodText =
                $"{fromDate?.ToString("dd MMM yyyy") ?? "Beginning"} - " +
                $"{toDate?.ToString("dd MMM yyyy") ?? "Today"}";


            var generatedText =
                DateTime.Now.ToString(
                    "dd MMM yyyy HH:mm");


            // ========================================================
            // GENERATE PDF
            // ========================================================

            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(
    PageSizes.A4.Landscape());

                        page.Margin(25);

                        page.DefaultTextStyle(x =>
                            x.FontSize(9));


                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text("HireTrack")
                                    .FontSize(11)
                                    .SemiBold();

                                column.Item()
                                    .PaddingTop(4)
                                    .Text("Recruitment Report")
                                    .FontSize(22)
                                    .Bold();

                                column.Item()
                                    .PaddingTop(4)
                                    .Text(
                                        $"Vacancy: {reportVacancyName}")
                                    .FontSize(10);

                                column.Item()
                                    .Text(
                                        $"Reporting Period: {periodText}")
                                    .FontSize(9);

                                column.Item()
                                    .Text(
                                        $"Generated: {generatedText}")
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken1);
                            });


                        page.Content()
                            .PaddingVertical(15)
                            .Column(column =>
                            {
                                column.Item()
                                    .Text("Recruitment Summary")
                                    .FontSize(14)
                                    .SemiBold();


                                column.Item()
                                    .PaddingTop(8)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });


                                        void SummaryCell(
                                            string title,
                                            int value)
                                        {
                                            table.Cell()
                                                .Border(1)
                                                .BorderColor(
                                                    Colors.Grey.Lighten2)
                                                .Padding(8)
                                                .Column(cell =>
                                                {
                                                    cell.Item()
                                                        .Text(title)
                                                        .FontSize(8)
                                                        .FontColor(
                                                            Colors.Grey.Darken1);

                                                    cell.Item()
                                                        .PaddingTop(3)
                                                        .Text(
                                                            value.ToString())
                                                        .FontSize(17)
                                                        .Bold();
                                                });
                                        }


                                        SummaryCell(
                                            "Applications",
                                            totalApplications);

                                        SummaryCell(
                                            "AI Passed",
                                            aiPassed);

                                        SummaryCell(
                                            "HR Reviewed",
                                            hrReviewed);

                                        SummaryCell(
                                            "Sent to HM",
                                            shortlisted);

                                        SummaryCell(
                                            "HM Pending",
                                            hmPending);

                                        SummaryCell(
                                            "HM Approved",
                                            hmApproved);

                                        SummaryCell(
                                            "HM Rejected",
                                            hmRejected);

                                        SummaryCell(
                                            "Total Interviews",
                                            totalInterviews);

                                        SummaryCell(
                                            "Completed Interviews",
                                            completedInterviews);

                                        SummaryCell(
                                            "Pending Decisions",
                                            pendingDecisions);

                                        SummaryCell(
                                            "Passed Rounds",
                                            passedRounds);

                                        SummaryCell(
                                            "Failed Rounds",
                                            failedRounds);
                                    });


                                column.Item()
                                    .PaddingTop(20)
                                    .Text("Vacancy Performance")
                                    .FontSize(14)
                                    .SemiBold();


                                column.Item()
                                    .PaddingTop(8)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.RelativeColumn(2.2f);
                                                columns.RelativeColumn(1.2f);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });


                                        table.Header(header =>
                                        {
                                            void HeaderCell(
                                                string text)
                                            {
                                                header.Cell()
                                                    .Background(
                                                        Colors.Grey.Lighten3)
                                                    .Border(1)
                                                    .BorderColor(
                                                        Colors.Grey.Lighten1)
                                                    .Padding(6)
                                                    .Text(text)
                                                    .SemiBold()
                                                    .FontSize(7);
                                            }


                                            HeaderCell("Vacancy");
                                            HeaderCell("Location");
                                            HeaderCell("Applications");
                                            HeaderCell("AI Passed");
                                            HeaderCell("HR Reviewed");
                                            HeaderCell("Sent to HM");
                                            HeaderCell("HM Approved");
                                            HeaderCell("Interviews");
                                            HeaderCell("Passed");
                                            HeaderCell("Failed");
                                        });


                                        foreach (var row
                                                 in vacancyRows)
                                        {
                                            void DataCell(
                                                string text)
                                            {
                                                table.Cell()
                                                    .BorderBottom(1)
                                                    .BorderColor(
                                                        Colors.Grey.Lighten2)
                                                    .Padding(6)
                                                    .Text(text)
                                                    .FontSize(7);
                                            }


                                            DataCell(
                                                row.JobTitle
                                                ?? "—");

                                            DataCell(
                                                row.Location
                                                ?? "—");

                                            DataCell(
                                                row.Applications
                                                    .ToString());

                                            DataCell(
                                                row.AIPassed
                                                    .ToString());

                                            DataCell(
                                                row.HRReviewed
                                                    .ToString());

                                            DataCell(
                                                row.Shortlisted
                                                    .ToString());

                                            DataCell(
                                                row.HMApproved
                                                    .ToString());

                                            DataCell(
                                                row.Interviews
                                                    .ToString());

                                            DataCell(
                                                row.PassedRounds
                                                    .ToString());

                                            DataCell(
                                                row.FailedRounds
                                                    .ToString());
                                        }
                                    });
                            });


                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "HireTrack Recruitment Report · Page ");

                                text.CurrentPageNumber();

                                text.Span(" of ");

                                text.TotalPages();
                            });
                    });
                });


            var pdfBytes =
                document.GeneratePdf();


            var fileName =
                $"HireTrack-Recruitment-Report-{DateTime.Now:yyyyMMdd-HHmm}.pdf";


            return File(
                pdfBytes,
                "application/pdf",
                fileName);
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
        [Authorize(Roles = "HR,HiringManager")]

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
        [Authorize(Roles = "HR,HiringManager")]

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
        [Authorize(Roles = "HR,HiringManager")]

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
        // SEND SELECTED PASSED CANDIDATES TO HIRING MANAGER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToHiringManager(
            List<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["Error"] =
                    "Please select at least one candidate.";

                return RedirectToAction(
                    nameof(Management),
                    new { status = "passed" });
            }

            // HRShortlisted is the existing handoff flag in this project.
            // true = the application has been sent to the Hiring Manager.
            var applications = await _context.JobApplications
                .Where(a =>
                    selectedIds.Contains(a.Id) &&
                    a.Status != "Not Shortlisted" &&
                    !a.HRShortlisted)
                .ToListAsync();

            if (applications.Count == 0)
            {
                TempData["Error"] =
                    "No valid candidates were selected, or they were already sent.";

                return RedirectToAction(
                    nameof(Management),
                    new { status = "passed" });
            }

            foreach (var application in applications)
            {
                application.HRReviewed = true;
                application.HRShortlisted = true;

                // Start a clean Hiring Manager review.
                application.HiringManagerReviewed = false;
                application.HiringManagerShortlisted = false;
                application.HiringManagerDecision = "Pending";
                application.HiringManagerComments = "";

                // Keep the candidate-facing pipeline simple.
                application.Status = "Shortlisted";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{applications.Count} candidate(s) sent to the Hiring Manager successfully.";

            return RedirectToAction(
                nameof(Management),
                new { status = "passed" });
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