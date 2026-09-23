using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.Json;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "Interviewer")]
    public class InterviewerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;


        public InterviewerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        // =========================================================
        // MY INTERVIEWS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MyInterviews()
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Challenge();
            }


            if (!interviewer.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    "Setup",
                    "InterviewerProfile"
                );
            }


            // =========================================================
            // 1. INTERVIEWS LINKED THROUGH INTERVIEWER SCHEDULE
            // =========================================================

            var scheduledInterviewIds =
                await _context.InterviewerSchedules

                    .Where(x =>
                        x.InterviewerId == interviewer.Id &&
                        !x.IsDeleted &&
                        x.IsSystemGenerated &&
                        x.InterviewId.HasValue)

                    .Select(x =>
                        x.InterviewId!.Value)

                    .Distinct()

                    .ToListAsync();


            // =========================================================
            // 2. LOAD INTERVIEWS
            //
            // Accept interview assignment from either:
            // - InterviewerSchedules
            // OR
            // - Interview.InterviewerIds
            // =========================================================

            var interviewerIdToken =
                "," + interviewer.Id + ",";


            var interviews =
                await _context.Interviews

                    .Include(x =>
                        x.JobApplication)

                        .ThenInclude(x =>
                            x.Candidate)

                    .Include(x =>
                        x.JobApplication)

                        .ThenInclude(x =>
                            x.JobVacancy)

                    .Include(x =>
                        x.InterviewRound)

                    .Include(x =>
                        x.InterviewType)

                    .Include(x =>
                        x.Assessments)

                        .ThenInclude(x =>
                            x.AssessmentTemplate)

                    .Where(x =>

                        // Assigned through calendar
                        scheduledInterviewIds.Contains(x.Id)

                        ||

                        // Assigned directly on Interview
                        (
                            x.InterviewerIds != null &&

                            (
                                "," + x.InterviewerIds + ","
                            ).Contains(
                                interviewerIdToken
                            )
                        )
                    )

                    .OrderByDescending(x =>
                        x.InterviewDate)

                    .ThenByDescending(x =>
                        x.InterviewTime)

                    .AsSplitQuery()

                    .ToListAsync();


            ViewBag.InterviewerName =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";


            return View(
                interviews
            );
        }


        // =========================================================
        // INTERVIEW DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> InterviewDetails(
            int id)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Challenge();
            }


            // Security:
            // interviewer can only open their assigned interview.
            var assigned =
                await _context.InterviewerSchedules

                    .AnyAsync(x =>
                        x.InterviewerId ==
                            interviewer.Id &&

                        x.InterviewId ==
                            id &&

                        x.IsSystemGenerated &&

                        !x.IsDeleted);


            if (!assigned)
            {
                return Forbid();
            }


            var interview =
                await _context.Interviews

                    .Include(x =>
                        x.JobApplication)

                        .ThenInclude(x =>
                            x.Candidate)

                    .Include(x =>
                        x.JobApplication)

                        .ThenInclude(x =>
                            x.JobVacancy)

                    .Include(x =>
                        x.InterviewRound)

                    .Include(x =>
                        x.InterviewType)

                    .Include(x =>
                        x.Assessments)

                        .ThenInclude(x =>
                            x.AssessmentTemplate)

                            .ThenInclude(x =>
                                x.Questions)

                    .Include(x =>
                        x.Assessments)

                        .ThenInclude(x =>
                            x.Answers)

                    .Include(x =>
                        x.Feedbacks)

                    .AsSplitQuery()

                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (interview == null)
            {
                return NotFound();
            }


            ViewBag.CurrentInterviewerId =
                interviewer.Id;


            ViewBag.InterviewerName =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";


            ViewBag.MyFeedback =
                interview.Feedbacks

                    .FirstOrDefault(x =>
                        x.InterviewerId ==
                            interviewer.Id);


            return View(
                interview
            );
        }


        // =========================================================
        // VIEW CANDIDATE CV
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewCV(int interviewId)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);

            if (interviewer == null)
            {
                return Challenge();
            }

            var assigned =
                await _context.InterviewerSchedules
                    .AnyAsync(x =>
                        x.InterviewerId == interviewer.Id &&
                        x.InterviewId == interviewId &&
                        x.IsSystemGenerated &&
                        !x.IsDeleted);

            if (!assigned)
            {
                return Forbid();
            }

            var interview =
                await _context.Interviews
                    .Include(x => x.JobApplication)
                    .FirstOrDefaultAsync(x => x.Id == interviewId);

            var application = interview?.JobApplication;

            if (application == null ||
                string.IsNullOrWhiteSpace(application.CVFilePath))
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
                GetContentType(fullPath),
                enableRangeProcessing: true
            );
        }


        // =========================================================
        // VIEW ORIGINAL AI ANALYSIS PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AIAnalysis(int interviewId)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);

            if (interviewer == null)
            {
                return Challenge();
            }

            var assigned =
                await _context.InterviewerSchedules
                    .AnyAsync(x =>
                        x.InterviewerId == interviewer.Id &&
                        x.InterviewId == interviewId &&
                        x.IsSystemGenerated &&
                        !x.IsDeleted);

            if (!assigned)
            {
                return Forbid();
            }

            var interview =
                await _context.Interviews
                    .Include(x => x.JobApplication)
                        .ThenInclude(x => x.Candidate)
                    .Include(x => x.JobApplication)
                        .ThenInclude(x => x.JobVacancy)
                    .FirstOrDefaultAsync(x => x.Id == interviewId);

            var application = interview?.JobApplication;

            if (application == null)
            {
                return NotFound();
            }

            var requiredResults =
                string.IsNullOrWhiteSpace(application.AIRequiredResults)
                    ? new List<AIAnalysisRequirement>()
                    : JsonSerializer.Deserialize<List<AIAnalysisRequirement>>(
                        application.AIRequiredResults)
                      ?? new List<AIAnalysisRequirement>();

            var preferredResults =
                string.IsNullOrWhiteSpace(application.AIPreferredResults)
                    ? new List<AIAnalysisRequirement>()
                    : JsonSerializer.Deserialize<List<AIAnalysisRequirement>>(
                        application.AIPreferredResults)
                      ?? new List<AIAnalysisRequirement>();

            var model = new AIAnalysisViewModel
            {
                Application = application,
                RequiredResults = requiredResults,
                PreferredResults = preferredResults
            };

            ViewBag.VacancyId = application.JobVacancyId;
            ViewBag.ReturnToInterviewId = interviewId;

            return View(
                "~/Views/HRApplication/AIAnalysis.cshtml",
                model
            );
        }


        // =========================================================
        // SUBMIT ASSESSMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssessment(
            int assessmentId,
            double? score,
            double? maxScore,
            string resultStatus,
            DateTime? assessmentDate,
            string? comments,
            List<int>? questionIds,
            List<string?>? answerTexts,
            List<double?>? answerScores)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Challenge();
            }


            var assessment =
                await _context
                    .Set<InterviewAssessment>()

                    .Include(x =>
                        x.Interview)

                    .Include(x =>
                        x.AssessmentTemplate)

                        .ThenInclude(x =>
                            x.Questions)

                    .Include(x =>
                        x.Answers)

                    .FirstOrDefaultAsync(x =>
                        x.Id == assessmentId);


            if (assessment == null)
            {
                return NotFound();
            }


            // =====================================================
            // CHECK INTERVIEWER IS ASSIGNED
            // =====================================================

            var assigned =
                await _context.InterviewerSchedules

                    .AnyAsync(x =>
                        x.InterviewerId ==
                            interviewer.Id &&

                        x.InterviewId ==
                            assessment.InterviewId &&

                        x.IsSystemGenerated &&

                        !x.IsDeleted);


            if (!assigned)
            {
                return Forbid();
            }


            // =====================================================
            // DON'T ALLOW ANOTHER INTERVIEWER TO OVERWRITE
            // A SUBMITTED ASSESSMENT
            // =====================================================

            if (assessment.Status == "Submitted" &&
                !string.IsNullOrWhiteSpace(
                    assessment.ConductedByInterviewerId) &&
                assessment.ConductedByInterviewerId !=
                    interviewer.Id)
            {
                TempData["Error"] =
                    "This assessment has already been submitted by another interviewer.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            assessment.InterviewId
                    }
                );
            }


            // =====================================================
            // VALIDATE SCORE
            // =====================================================

            if (score.HasValue &&
                score.Value < 0)
            {
                TempData["Error"] =
                    "Assessment score cannot be negative.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            assessment.InterviewId
                    }
                );
            }


            if (maxScore.HasValue &&
                maxScore.Value <= 0)
            {
                TempData["Error"] =
                    "Maximum score must be greater than zero.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            assessment.InterviewId
                    }
                );
            }


            if (score.HasValue &&
                maxScore.HasValue &&
                score.Value > maxScore.Value)
            {
                TempData["Error"] =
                    "Assessment score cannot be greater than maximum score.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            assessment.InterviewId
                    }
                );
            }


            // =====================================================
            // VALIDATE RESULT
            // =====================================================

            var allowedResults =
                new[]
                {
                    "Pass",
                    "Fail"
                };


            if (!allowedResults.Contains(
                resultStatus))
            {
                TempData["Error"] =
                    "Please select a valid assessment result.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            assessment.InterviewId
                    }
                );
            }


            // =====================================================
            // SAVE MAIN ASSESSMENT
            // =====================================================

            assessment.Score =
                score;


            assessment.MaxScore =
                maxScore;


            assessment.ResultStatus =
                resultStatus;


            assessment.AssessmentDate =
                assessmentDate
                ?? DateTime.Today;


            assessment.Comments =
                string.IsNullOrWhiteSpace(
                    comments)

                    ? null

                    : comments.Trim();


            assessment.ConductedByInterviewerId =
                interviewer.Id;


            assessment.Status =
                "Submitted";


            assessment.SubmittedDate =
                DateTime.Now;


            // =====================================================
            // REMOVE OLD ANSWERS
            // =====================================================

            if (assessment.Answers.Any())
            {
                _context
                    .Set<AssessmentAnswer>()

                    .RemoveRange(
                        assessment.Answers
                    );
            }


            // =====================================================
            // VALID QUESTION IDS
            // =====================================================

            var validQuestionIds =
                assessment
                    .AssessmentTemplate
                    ?.Questions

                    .Select(x =>
                        x.Id)

                    .ToHashSet()

                ?? new HashSet<int>();


            // =====================================================
            // SAVE ANSWERS
            // =====================================================

            if (questionIds != null)
            {
                for (var i = 0;
                     i < questionIds.Count;
                     i++)
                {
                    var questionId =
                        questionIds[i];


                    if (!validQuestionIds.Contains(
                        questionId))
                    {
                        continue;
                    }


                    string? answerText =
                        null;


                    double? answerScore =
                        null;


                    if (answerTexts != null &&
                        i < answerTexts.Count)
                    {
                        answerText =
                            answerTexts[i];
                    }


                    if (answerScores != null &&
                        i < answerScores.Count)
                    {
                        answerScore =
                            answerScores[i];
                    }


                    var answer =
                        new AssessmentAnswer
                        {
                            InterviewAssessmentId =
                                assessment.Id,

                            AssessmentQuestionId =
                                questionId,

                            AnswerText =
                                string.IsNullOrWhiteSpace(
                                    answerText)

                                    ? null

                                    : answerText.Trim(),

                            Score =
                                answerScore
                        };


                    _context
                        .Set<AssessmentAnswer>()

                        .Add(
                            answer
                        );
                }
            }


            // Interview is now being worked on.
            if (assessment.Interview != null &&
                assessment.Interview.Status ==
                    "Scheduled")
            {
                assessment.Interview.Status =
                    "In Progress";
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Assessment submitted successfully.";


            return RedirectToAction(
                nameof(InterviewDetails),
                new
                {
                    id =
                        assessment.InterviewId
                }
            );
        }


        // =========================================================
        // SUBMIT INTERVIEW FEEDBACK
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(
            int interviewId,
            int technicalRating,
            int problemSolvingRating,
            int communicationRating,
            int roleFitRating,
            int professionalismRating,
            int overallRating,
            string? strengths,
            string? weaknesses,
            string? comments,
            string recommendation)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Challenge();
            }


            // =====================================================
            // SECURITY CHECK
            // =====================================================

            var assigned =
                await _context.InterviewerSchedules

                    .AnyAsync(x =>
                        x.InterviewerId ==
                            interviewer.Id &&

                        x.InterviewId ==
                            interviewId &&

                        x.IsSystemGenerated &&

                        !x.IsDeleted);


            if (!assigned)
            {
                return Forbid();
            }


            // =====================================================
            // RATING VALIDATION
            // =====================================================

            var ratings =
                new[]
                {
                    technicalRating,
                    problemSolvingRating,
                    communicationRating,
                    roleFitRating,
                    professionalismRating,
                    overallRating
                };


            if (ratings.Any(x =>
                x < 1 ||
                x > 5))
            {
                TempData["Error"] =
                    "All ratings must be between 1 and 5.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            interviewId
                    }
                );
            }


            // =====================================================
            // RECOMMENDATION VALIDATION
            // =====================================================

            var recommendations =
                new[]
                {
                    "Strong Pass",
                    "Pass",
                    "Borderline",
                    "Fail"
                };


            if (!recommendations.Contains(
                recommendation))
            {
                TempData["Error"] =
                    "Please select a valid recommendation.";


                return RedirectToAction(
                    nameof(InterviewDetails),
                    new
                    {
                        id =
                            interviewId
                    }
                );
            }


            // =====================================================
            // GET EXISTING FEEDBACK
            // =====================================================

            var feedback =
                await _context.Set<InterviewFeedback>()

                    .FirstOrDefaultAsync(x =>
                        x.InterviewId ==
                            interviewId &&

                        x.InterviewerId ==
                            interviewer.Id);


            // =====================================================
            // CREATE IF FIRST SUBMISSION
            // =====================================================

            if (feedback == null)
            {
                feedback =
                    new InterviewFeedback
                    {
                        InterviewId =
                            interviewId,

                        InterviewerId =
                            interviewer.Id
                    };


                _context.Set<InterviewFeedback>()
                    .Add(
                        feedback
                    );
            }


            // =====================================================
            // SAVE FEEDBACK
            // =====================================================

            feedback.TechnicalRating =
                technicalRating;


            feedback.ProblemSolvingRating =
                problemSolvingRating;


            feedback.CommunicationRating =
                communicationRating;


            feedback.RoleFitRating =
                roleFitRating;


            feedback.ProfessionalismRating =
                professionalismRating;


            feedback.OverallRating =
                overallRating;


            feedback.Strengths =
                strengths?.Trim()
                ?? "";


            feedback.Weaknesses =
                weaknesses?.Trim()
                ?? "";


            feedback.Comments =
                comments?.Trim()
                ?? "";


            feedback.Recommendation =
                recommendation;


            feedback.SubmittedDate =
                DateTime.Now;


            await _context.SaveChangesAsync();


            // =====================================================
            // UPDATE INTERVIEW FEEDBACK STATUS
            // =====================================================

            var interview =
                await _context.Interviews

                    .Include(x =>
                        x.Assessments)

                    .Include(x =>
                        x.Feedbacks)

                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            interviewId);


            if (interview != null)
            {
                var assignedInterviewerIds =
                    (interview.InterviewerIds
                        ?? "")

                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries
                    )

                    .Select(x =>
                        x.Trim())

                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))

                    .Distinct()

                    .ToList();


                var feedbackInterviewerIds =
                    interview.Feedbacks

                        .Select(x =>
                            x.InterviewerId)

                        .Distinct()

                        .ToList();


                var allFeedbackSubmitted =
                    assignedInterviewerIds.Count > 0 &&

                    assignedInterviewerIds.All(
                        id =>
                            feedbackInterviewerIds.Contains(id)
                    );


                var allAssessmentsSubmitted =
                    !interview.Assessments.Any()

                    ||

                    interview.Assessments.All(
                        x =>
                            x.Status == "Submitted"
                    );


                if (allFeedbackSubmitted)
                {
                    interview.FeedbackStatus =
                        "Submitted";
                }
                else
                {
                    interview.FeedbackStatus =
                        "In Progress";
                }


                // Individual interview is complete only when
                // assessment(s) and interviewer feedback are done.
                // HM still makes the actual ROUND Pass/Fail decision.
                if (allFeedbackSubmitted &&
                    allAssessmentsSubmitted)
                {
                    interview.Status =
                        "Completed";


                    interview.CompletedDate =
                        DateTime.Now;
                }
                else
                {
                    interview.Status =
                        "In Progress";
                }


                await _context.SaveChangesAsync();
            }


            TempData["Success"] =
                "Interview feedback submitted successfully.";


            return RedirectToAction(
                nameof(InterviewDetails),
                new
                {
                    id =
                        interviewId
                }
            );
        }


        // =========================================================
        // MY CALENDAR
        // =========================================================

        public async Task<IActionResult> Calendar()
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Challenge();
            }


            if (!interviewer.InterviewerProfileCompleted)
            {
                return RedirectToAction(
                    "Setup",
                    "InterviewerProfile"
                );
            }


            ViewBag.InterviewerName =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";


            ViewBag.JobTitle =
                interviewer.JobTitle
                ?? "";


            ViewBag.Department =
                interviewer.Department
                ?? "";


            return View();
        }


        // =========================================================
        // GET MY CALENDAR EVENTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetEvents(
            DateTime start,
            DateTime end)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Unauthorized();
            }


            var events =
                await _context.InterviewerSchedules

                    .Where(x =>
                        x.InterviewerId ==
                            interviewer.Id &&

                        !x.IsDeleted &&

                        x.StartDateTime <
                            end &&

                        x.EndDateTime >
                            start)

                    .OrderBy(x =>
                        x.StartDateTime)

                    .Select(x =>
                        new
                        {
                            id =
                                x.Id,

                            title =
                                x.Title
                                ?? x.ScheduleType,

                            start =
                                x.StartDateTime,

                            end =
                                x.EndDateTime,

                            allDay =
                                x.IsAllDay,

                            scheduleType =
                                x.ScheduleType,

                            notes =
                                x.Notes,

                            isSystemGenerated =
                                x.IsSystemGenerated,

                            interviewId =
                                x.InterviewId,

                            createdBy =
                                x.CreatedBy,

                            createdAt =
                                x.CreatedAt,

                            updatedBy =
                                x.UpdatedBy,

                            updatedAt =
                                x.UpdatedAt
                        })

                    .ToListAsync();


            return Json(
                events
            );
        }


        // =========================================================
        // ADD CALENDAR EVENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(
            DateTime start,
            DateTime end,
            string scheduleType,
            string? title,
            string? notes,
            bool isAllDay = false)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Unauthorized();
            }


            if (end <= start)
            {
                return BadRequest(
                    "End time must be after the start time."
                );
            }


            var allowedTypes =
                new[]
                {
                    "Meeting",
                    "Training",
                    "Leave",
                    "Personal",
                    "CompanyEvent",
                    "Other"
                };


            if (!allowedTypes.Contains(
                scheduleType))
            {
                return BadRequest(
                    "Invalid schedule type."
                );
            }


            var schedule =
                new InterviewerSchedule
                {
                    InterviewerId =
                        interviewer.Id,

                    StartDateTime =
                        start,

                    EndDateTime =
                        end,

                    IsAllDay =
                        isAllDay,

                    ScheduleType =
                        scheduleType,

                    Title =
                        string.IsNullOrWhiteSpace(
                            title)

                            ? scheduleType

                            : title.Trim(),

                    Notes =
                        string.IsNullOrWhiteSpace(
                            notes)

                            ? null

                            : notes.Trim(),

                    IsSystemGenerated =
                        false,

                    CreatedBy =
                        interviewer.FullName
                        ?? interviewer.Email
                        ?? "Interviewer",

                    CreatedAt =
                        DateTime.Now,

                    IsDeleted =
                        false
                };


            _context.InterviewerSchedules
                .Add(
                    schedule
                );


            await _context.SaveChangesAsync();


            return Json(
                new
                {
                    success =
                        true,

                    message =
                        "Schedule created successfully."
                }
            );
        }


        // =========================================================
        // UPDATE CALENDAR EVENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEvent(
            int id,
            DateTime start,
            DateTime end,
            string scheduleType,
            string? title,
            string? notes,
            bool isAllDay = false)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Unauthorized();
            }


            var schedule =
                await _context.InterviewerSchedules

                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            id &&

                        x.InterviewerId ==
                            interviewer.Id &&

                        !x.IsDeleted);


            if (schedule == null)
            {
                return NotFound();
            }


            if (schedule.IsSystemGenerated)
            {
                return BadRequest(
                    "Scheduled interviews cannot be edited."
                );
            }


            if (end <= start)
            {
                return BadRequest(
                    "End time must be after the start time."
                );
            }


            var allowedTypes =
                new[]
                {
                    "Meeting",
                    "Training",
                    "Leave",
                    "Personal",
                    "CompanyEvent",
                    "Other"
                };


            if (!allowedTypes.Contains(
                scheduleType))
            {
                return BadRequest(
                    "Invalid schedule type."
                );
            }


            schedule.StartDateTime =
                start;


            schedule.EndDateTime =
                end;


            schedule.IsAllDay =
                isAllDay;


            schedule.ScheduleType =
                scheduleType;


            schedule.Title =
                string.IsNullOrWhiteSpace(
                    title)

                    ? scheduleType

                    : title.Trim();


            schedule.Notes =
                string.IsNullOrWhiteSpace(
                    notes)

                    ? null

                    : notes.Trim();


            schedule.UpdatedBy =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";


            schedule.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            return Json(
                new
                {
                    success =
                        true,

                    message =
                        "Schedule updated successfully."
                }
            );
        }


        // =========================================================
        // DELETE CALENDAR EVENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(
            int id)
        {
            var interviewer =
                await _userManager.GetUserAsync(User);


            if (interviewer == null)
            {
                return Unauthorized();
            }


            var schedule =
                await _context.InterviewerSchedules

                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            id &&

                        x.InterviewerId ==
                            interviewer.Id &&

                        !x.IsDeleted);


            if (schedule == null)
            {
                return NotFound();
            }


            // Locked interview events cannot be deleted
            // manually by the interviewer.
            if (schedule.IsSystemGenerated)
            {
                return BadRequest(
                    "Scheduled interviews cannot be deleted."
                );
            }


            schedule.IsDeleted =
                true;


            schedule.DeletedBy =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";


            schedule.DeletedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            return Json(
                new
                {
                    success =
                        true,

                    message =
                        "Schedule deleted successfully."
                }
            );
        }

        // =========================================================
        // FILE HELPERS
        // =========================================================

        private string? GetFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            var relativePath = filePath
                .TrimStart('~', '/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var webRootPath =
                Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(webRootPath))
            {
                return webRootPath;
            }

            var contentRootPath =
                Path.Combine(_environment.ContentRootPath, relativePath);

            if (System.IO.File.Exists(contentRootPath))
            {
                return contentRootPath;
            }

            return null;
        }


        private string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
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