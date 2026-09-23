using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.RegularExpressions;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class InterviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // MAIN INTERVIEW PAGE
        // /Interview
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var interviews =
                await _context.Interviews

                    .Include(x => x.JobApplication)
                        .ThenInclude(x => x.Candidate)

                    .Include(x => x.JobApplication)
                        .ThenInclude(x => x.JobVacancy)

                    .Include(x => x.InterviewRound)

                    .Include(x => x.InterviewType)

                    .OrderByDescending(x =>
                        x.InterviewDate)

                    .ThenByDescending(x =>
                        x.InterviewTime)

                    .ToListAsync();


            return View(interviews);
        }


        // =========================================================
        // OPEN SCHEDULE INTERVIEW MODAL
        // /Interview/Schedule
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            // -----------------------------------------------------
            // ACTIVE JOBS
            // -----------------------------------------------------

            var jobs =
                await _context.JobVacancies

                    .Where(x =>
                        x.IsActive)

                    .OrderBy(x =>
                        x.JobTitle)

                    .ToListAsync();


            // -----------------------------------------------------
            // INTERVIEW TYPES
            // -----------------------------------------------------

            var interviewTypes =
                await _context.InterviewTypes

                    .OrderBy(x =>
                        x.Name)

                    .ToListAsync();


            // -----------------------------------------------------
            // We DO NOT show interviewers here directly anymore.
            //
            // They will be loaded later using
            // GetAvailableInterviewers()
            // after date/time/job/duration are selected.
            // -----------------------------------------------------

            var model =
                new ScheduleInterviewViewModel
                {
                    Jobs =
                        jobs
                            .Select(x =>
                                new SelectListItem
                                {
                                    Value =
                                        x.Id.ToString(),

                                    Text =
                                        x.JobTitle
                                })
                            .ToList(),


                    InterviewTypes =
                        interviewTypes
                            .Select(x =>
                                new SelectListItem
                                {
                                    Value =
                                        x.Id.ToString(),

                                    Text =
                                        x.Name
                                })
                            .ToList(),


                    // Keep empty because cards are loaded dynamically.
                    Interviewers =
                        new List<SelectListItem>(),


                    InterviewDate =
                        DateTime.Today,


                    DurationMinutes =
                        60
                };


            return PartialView(
                "_ScheduleInterview",
                model
            );
        }

        // =========================================================
        // SAVE / SCHEDULE INTERVIEWS
        // =========================================================

        // =========================================================
        // SAVE / SCHEDULE INTERVIEWS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(
            ScheduleInterviewViewModel model)
        {
            // =====================================================
            // 1. BASIC VALIDATION
            // =====================================================

            if (model.SelectedApplicationIds == null ||
                model.SelectedApplicationIds.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedApplicationIds),
                    "Please select at least one candidate."
                );
            }


            if (model.SelectedInterviewerIds == null ||
                model.SelectedInterviewerIds.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedInterviewerIds),
                    "Please select at least one interviewer."
                );
            }


            if (model.JobVacancyId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.JobVacancyId),
                    "Please select a job vacancy."
                );
            }


            if (model.InterviewRoundId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewRoundId),
                    "Please select an interview round."
                );
            }


            if (model.InterviewTypeId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewTypeId),
                    "Please select an interview type."
                );
            }


            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.DurationMinutes),
                    "Please enter a valid duration."
                );
            }


            // =====================================================
            // 2. FIND VACANCY
            // =====================================================

            var vacancy =
                await _context.JobVacancies
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.JobVacancyId);


            if (vacancy == null)
            {
                ModelState.AddModelError(
                    "",
                    "Selected job vacancy could not be found."
                );
            }


            // =====================================================
            // 3. VALIDATE INTERVIEW ROUND
            // =====================================================

            var validRound =
                await _context.InterviewRounds
                    .AnyAsync(x =>
                        x.Id == model.InterviewRoundId &&
                        x.JobVacancyId == model.JobVacancyId &&
                        x.IsActive);


            if (!validRound)
            {
                ModelState.AddModelError(
                    nameof(model.InterviewRoundId),
                    "The selected interview round is not valid for this vacancy."
                );
            }


            // =====================================================
            // 4. VALIDATE ASSESSMENT
            //
            // Assessment is optional, but when selected it MUST:
            // - belong to this vacancy
            // - belong to this round
            // - be active
            // =====================================================

            if (model.AssessmentTemplateId.HasValue)
            {
                var validAssessment =
                    await _context
                        .Set<AssessmentTemplate>()

                        .AnyAsync(x =>
                            x.Id ==
                                model.AssessmentTemplateId.Value &&

                            x.JobVacancyId ==
                                model.JobVacancyId &&

                            x.InterviewRoundId ==
                                model.InterviewRoundId &&

                            x.IsActive);


                if (!validAssessment)
                {
                    ModelState.AddModelError(
                        nameof(model.AssessmentTemplateId),
                        "The selected assessment is not valid for this interview round."
                    );
                }
            }


            // =====================================================
            // 5. INTERVIEW START / END
            // =====================================================

            var interviewStart =
                model.InterviewDate.Date
                    .Add(model.InterviewTime);


            var interviewEnd =
                interviewStart.AddMinutes(
                    model.DurationMinutes
                );


            // =====================================================
            // 6. RECHECK INTERVIEWERS
            //
            // Never trust only the browser result.
            // Recheck:
            // - user exists
            // - eligible for vacancy
            // - still available
            // =====================================================

            if (vacancy != null &&
                model.SelectedInterviewerIds != null)
            {
                foreach (var interviewerId
                         in model.SelectedInterviewerIds)
                {
                    var interviewer =
                        await _userManager
                            .FindByIdAsync(
                                interviewerId
                            );


                    if (interviewer == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "One of the selected interviewers could not be found."
                        );

                        break;
                    }


                    if (!IsEligibleInterviewer(
                        interviewer,
                        vacancy))
                    {
                        ModelState.AddModelError(
                            "",
                            $"{interviewer.FullName ?? interviewer.Email} is no longer eligible for this vacancy."
                        );

                        break;
                    }


                    var conflict =
    await _context.InterviewerSchedules
        .AnyAsync(x =>

            x.InterviewerId ==
                interviewerId

            &&

            !x.IsDeleted

            &&

            x.IsSystemGenerated

            &&

            x.ScheduleType == "Interview"

            &&

            x.InterviewId != null

            &&

            x.StartDateTime <
                interviewEnd

            &&

            x.EndDateTime >
                interviewStart
        );


                    if (conflict)
                    {
                        ModelState.AddModelError(
                            "",
                            $"{interviewer.FullName ?? interviewer.Email} is no longer available at this time."
                        );

                        break;
                    }
                }
            }


            // =====================================================
            // 7. VALIDATION FAILED
            // =====================================================

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(x =>
                                x.Errors)
                            .Select(x =>
                                x.ErrorMessage)
                    );


                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // 8. ONLY VALID HM-APPROVED APPLICATIONS
            // =====================================================

            var validApplications =
                await _context.JobApplications

                    .Where(a =>

                        model.SelectedApplicationIds!
                            .Contains(a.Id)

                        &&

                        a.JobVacancyId ==
                            model.JobVacancyId

                        &&

                        a.HRShortlisted

                        &&

                        a.HiringManagerDecision ==
                            "ApprovedForInterview"
                    )

                    .ToListAsync();


            if (validApplications.Count == 0)
            {
                TempData["Error"] =
                    "No valid candidates were selected.";


                return RedirectToAction(
                    nameof(Index)
                );
            }

            // =====================================================
            // 8.1 VALIDATE INTERVIEW ROUND PROGRESSION
            //
            // Rules:
            // - Candidate must start with the first active round.
            // - A later round is allowed only when ALL earlier
            //   active rounds were passed by the Hiring Manager.
            // - Failed candidates cannot continue.
            // - Completed candidates cannot be scheduled again.
            // - Same round cannot be scheduled twice.
            // =====================================================

            var activeRounds =
                await _context.InterviewRounds

                    .Where(x =>
                        x.JobVacancyId ==
                            model.JobVacancyId

                        &&

                        x.IsActive)

                    .OrderBy(x =>
                        x.SequenceNumber)

                    .ToListAsync();


            var selectedRound =
                activeRounds.FirstOrDefault(x =>
                    x.Id ==
                        model.InterviewRoundId);


            if (selectedRound == null)
            {
                TempData["Error"] =
                    "The selected interview round is not active.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            foreach (var application
                     in validApplications)
            {
                // -------------------------------------------------
                // FAILED CANDIDATE
                // -------------------------------------------------

                if (application.InterviewStageStatus ==
                    "Failed")
                {
                    TempData["Error"] =
                        "A failed candidate cannot be scheduled for another interview round.";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }


                // -------------------------------------------------
                // COMPLETED CANDIDATE
                // -------------------------------------------------

                if (application.InterviewStageStatus ==
                    "Completed" ||
                    application.InterviewProcessCompleted)
                {
                    TempData["Error"] =
                        "This candidate has already completed the interview process.";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }


                // -------------------------------------------------
                // GET CANDIDATE'S EXISTING INTERVIEWS
                // -------------------------------------------------

                var candidateInterviews =
                    await _context.Interviews

                        .Include(x =>
                            x.InterviewRound)

                        .Where(x =>
                            x.JobApplicationId ==
                                application.Id)

                        .ToListAsync();


                // -------------------------------------------------
                // PREVENT DUPLICATE ROUND
                // -------------------------------------------------

                var sameRoundAlreadyExists =
                    candidateInterviews.Any(x =>

                        x.InterviewRoundId ==
                            model.InterviewRoundId

                        &&

                        x.Status !=
                            "Cancelled"
                    );


                if (sameRoundAlreadyExists)
                {
                    TempData["Error"] =
                        $"An interview for {selectedRound.Name} has already been scheduled for this candidate.";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }


                // -------------------------------------------------
                // FIND ALL EARLIER ACTIVE ROUNDS
                // -------------------------------------------------

                var previousRounds =
                    activeRounds

                        .Where(x =>
                            x.SequenceNumber <
                                selectedRound.SequenceNumber)

                        .ToList();


                // -------------------------------------------------
                // FIRST ROUND
                //
                // If there are no earlier rounds, it is allowed.
                // -------------------------------------------------

                if (previousRounds.Count == 0)
                {
                    continue;
                }


                // -------------------------------------------------
                // ALL PREVIOUS ROUNDS MUST HAVE PASSED
                // -------------------------------------------------

                foreach (var previousRound
                         in previousRounds)
                {
                    var previousInterview =
                        candidateInterviews

                            .Where(x =>
                                x.InterviewRoundId ==
                                    previousRound.Id)

                            .OrderByDescending(x =>
                                x.InterviewDate)

                            .ThenByDescending(x =>
                                x.InterviewTime)

                            .FirstOrDefault();


                    if (previousInterview == null)
                    {
                        TempData["Error"] =
                            $"The candidate must complete Round {previousRound.SequenceNumber} — {previousRound.Name} before scheduling {selectedRound.Name}.";

                        return RedirectToAction(
                            nameof(Index)
                        );
                    }


                    if (previousInterview.RoundResult !=
                        "Passed")
                    {
                        TempData["Error"] =
                            $"Round {previousRound.SequenceNumber} — {previousRound.Name} must be passed by the Hiring Manager before the next round can be scheduled.";

                        return RedirectToAction(
                            nameof(Index)
                        );
                    }
                }
            }


            // =====================================================
            // 9. CURRENT HR USER
            // =====================================================

            var currentUser =
                await _userManager
                    .GetUserAsync(User);


            var hrName =
                currentUser?.FullName
                ?? currentUser?.Email
                ?? "HR";


            // =====================================================
            // 10. CREATE ONE INTERVIEW FOR EACH CANDIDATE
            // =====================================================

            foreach (var application
                     in validApplications)
            {
                // =================================================
                // CREATE INTERVIEW
                // =================================================

                var interview =
                    new Interview
                    {
                        JobApplicationId =
                            application.Id,

                        InterviewRoundId =
                            model.InterviewRoundId,

                        InterviewTypeId =
                            model.InterviewTypeId,

                        InterviewDate =
                            model.InterviewDate.Date,

                        InterviewTime =
                            model.InterviewTime,

                        DurationMinutes =
                            model.DurationMinutes,

                        Location =
                            string.IsNullOrWhiteSpace(
                                model.Location)

                                ? null

                                : model.Location.Trim(),

                        MeetingLink =
                            string.IsNullOrWhiteSpace(
                                model.MeetingLink)

                                ? null

                                : model.MeetingLink.Trim(),

                        InterviewerIds =
                            string.Join(
                                ",",
                                model.SelectedInterviewerIds!
                            ),

                        Status =
                            "Scheduled",

                        FeedbackStatus =
                            "Pending",

                        RoundResult =
                            "Pending",

                        CreatedDate =
                            DateTime.Now
                    };


                _context.Interviews.Add(
                    interview
                );


                // -------------------------------------------------
                // Save now because we need interview.Id below.
                // -------------------------------------------------

                await _context.SaveChangesAsync();


                // =================================================
                // ASSIGN ASSESSMENT TO THIS INTERVIEW
                // =================================================

                if (model.AssessmentTemplateId.HasValue)
                {
                    var interviewAssessment =
                        new InterviewAssessment
                        {
                            InterviewId =
                                interview.Id,

                            AssessmentTemplateId =
                                model.AssessmentTemplateId.Value,

                            AssignedBy =
                                hrName,

                            AssignedDate =
                                DateTime.Now,

                            Status =
                                "Pending",

                            ResultStatus =
                                "Pending",

                            Score =
                                null,

                            MaxScore =
                                null,

                            AssessmentDate =
                                null,

                            Comments =
                                null,

                            ConductedByInterviewerId =
                                null,

                            SubmittedDate =
                                null
                        };


                    _context
                        .Set<InterviewAssessment>()
                        .Add(
                            interviewAssessment
                        );
                }


                // =================================================
                // ADD INTERVIEW TO EACH INTERVIEWER'S CALENDAR
                //
                // This MUST stay inside this candidate loop
                // because each calendar event needs this
                // particular interview.Id.
                // =================================================

                foreach (var interviewerId
                         in model.SelectedInterviewerIds!)
                {
                    var calendarEvent =
                        new InterviewerSchedule
                        {
                            InterviewerId =
                                interviewerId,

                            StartDateTime =
                                interviewStart,

                            EndDateTime =
                                interviewEnd,

                            IsAllDay =
                                false,

                            ScheduleType =
                                "Interview",

                            Title =
                                "Candidate Interview",

                            Notes =
                                $"Interview for application #{application.Id}",

                            IsSystemGenerated =
                                true,

                            InterviewId =
                                interview.Id,

                            IsDeleted =
                                false,

                            CreatedBy =
                                hrName,

                            CreatedAt =
                                DateTime.Now
                        };


                    _context.InterviewerSchedules
                        .Add(
                            calendarEvent
                        );
                }


                // =================================================
                // UPDATE CANDIDATE'S OVERALL INTERVIEW STATUS
                // =================================================
                //
                // First interview:
                // Not Started -> Scheduled
                //
                // If candidate has already passed an earlier round
                // and is In Progress, don't change it back.
                // =================================================

                if (string.IsNullOrWhiteSpace(
                        application.InterviewStageStatus)
                    ||
                    application.InterviewStageStatus ==
                        "Not Started")
                {
                    application.InterviewStageStatus =
                        "Scheduled";
                }


                application.InterviewProcessCompleted =
                    false;


                // =================================================
                // SAVE ASSESSMENT + CALENDAR + APPLICATION STATUS
                // =================================================

                await _context.SaveChangesAsync();
            }


            // =====================================================
            // 11. SUCCESS
            // =====================================================

            TempData["Success"] =
                $"{validApplications.Count} interview(s) scheduled successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }

        // =========================================================
        // GET CANDIDATES
        //
        // Only candidates:
        // HR shortlisted
        // AND
        // Hiring Manager approved for interview
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetCandidates(
            int jobId)
        {
            var candidates =
                await _context.JobApplications

                    .Include(x =>
                        x.Candidate)

                    .Where(x =>

                        x.JobVacancyId ==
                            jobId

                        &&

                        x.HRShortlisted ==
                            true

                        &&

                        x.HiringManagerDecision ==
                            "ApprovedForInterview"
                    )

                    .OrderByDescending(x =>
                        x.AIScore)

                    .Select(x =>
                        new
                        {
                            applicationId =
                                x.Id,

                            name =
                                x.Candidate != null
                                    ? x.Candidate.FullName
                                    : "Unknown Candidate",

                            email =
                                x.Candidate != null
                                    ? x.Candidate.Email
                                    : "",

                            aiScore =
                                x.AIScore
                        })

                    .ToListAsync();


            return Json(candidates);
        }


        // =========================================================
        // GET INTERVIEW ROUNDS FOR SELECTED JOB
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetRounds(
            int jobId)
        {
            var rounds =
                await _context.InterviewRounds

                    .Where(x =>

                        x.JobVacancyId ==
                            jobId

                        &&

                        x.IsActive
                    )

                    .OrderBy(x =>
                        x.SequenceNumber)

                    .Select(x =>
                        new
                        {
                            id =
                                x.Id,

                            name =
                                x.Name,

                            sequence =
                                x.SequenceNumber
                        })

                    .ToListAsync();


            return Json(rounds);
        }


        // =========================================================
        // GET ASSESSMENTS FOR JOB + ROUND
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetAssessments(
            int jobId,
            int roundId)
        {
            var assessments =
                await _context
                    .Set<AssessmentTemplate>()

                    .Where(x =>
                        x.JobVacancyId == jobId &&
                        x.InterviewRoundId == roundId &&
                        x.IsActive)

                    .OrderBy(x => x.Name)

                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name
                    })

                    .ToListAsync();


            return Json(assessments);
        }

        // =========================================================
        // EDIT INTERVIEW - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var interview =
                await _context.Interviews

                .Include(x => x.JobApplication)
                    .ThenInclude(x => x.Candidate)

                .Include(x => x.JobApplication)
                    .ThenInclude(x => x.JobVacancy)

                .Include(x => x.InterviewRound)

                .Include(x => x.InterviewType)

                .FirstOrDefaultAsync(x =>
                    x.Id == id);


            if (interview == null)
            {
                return NotFound();
            }


            if (interview.Status == "Cancelled")
            {
                TempData["Error"] =
                    "A cancelled interview cannot be edited.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        status = "Cancelled"
                    }
                );
            }


            ViewBag.InterviewRounds =
                await _context.InterviewRounds

                .Where(x =>
                    x.JobVacancyId ==
                        interview.JobApplication!.JobVacancyId &&
                    x.IsActive)

                .OrderBy(x =>
                    x.SequenceNumber)

                .Select(x =>
                    new SelectListItem
                    {
                        Value =
                            x.Id.ToString(),

                        Text =
                            $"Round {x.SequenceNumber} — {x.Name}",

                        Selected =
                            x.Id ==
                            interview.InterviewRoundId
                    })

                .ToListAsync();


            ViewBag.InterviewTypes =
                await _context.InterviewTypes

                .Where(x =>
                    x.IsActive)

                .OrderBy(x =>
                    x.Name)

                .Select(x =>
                    new SelectListItem
                    {
                        Value =
                            x.Id.ToString(),

                        Text =
                            x.Name,

                        Selected =
                            x.Id ==
                            interview.InterviewTypeId
                    })

                .ToListAsync();


            return View(interview);
        }


        // =========================================================
        // EDIT INTERVIEW - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Interview model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }


            var interview =
                await _context.Interviews

                .Include(x =>
                    x.JobApplication)

                .FirstOrDefaultAsync(x =>
                    x.Id == id);


            if (interview == null)
            {
                return NotFound();
            }


            if (interview.Status == "Cancelled")
            {
                TempData["Error"] =
                    "A cancelled interview cannot be edited.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        status = "Cancelled"
                    }
                );
            }


            // =====================================================
            // OLD TIME RANGE
            // =====================================================

            var oldStart =
                interview.InterviewDate.Date
                    .Add(
                        interview.InterviewTime
                    );


            var oldEnd =
                oldStart.AddMinutes(
                    interview.DurationMinutes
                );


            // =====================================================
            // NEW TIME RANGE
            // =====================================================

            var newStart =
                model.InterviewDate.Date
                    .Add(
                        model.InterviewTime
                    );


            var newEnd =
                newStart.AddMinutes(
                    model.DurationMinutes
                );


            if (newEnd <= newStart)
            {
                TempData["Error"] =
                    "Interview end time must be after the start time.";

                return RedirectToAction(
                    nameof(Edit),
                    new
                    {
                        id
                    }
                );
            }


            // =====================================================
            // UPDATE INTERVIEW
            // =====================================================

            interview.InterviewDate =
                model.InterviewDate.Date;


            interview.InterviewTime =
                model.InterviewTime;


            interview.DurationMinutes =
                model.DurationMinutes;


            interview.Location =
                string.IsNullOrWhiteSpace(
                    model.Location)

                    ? null

                    : model.Location.Trim();


            interview.MeetingLink =
                string.IsNullOrWhiteSpace(
                    model.MeetingLink)

                    ? null

                    : model.MeetingLink.Trim();


            interview.InterviewRoundId =
                model.InterviewRoundId;


            interview.InterviewTypeId =
                model.InterviewTypeId;


            // =====================================================
            // UPDATE INTERVIEWER CALENDAR EVENTS TOO
            // =====================================================

            var calendarEvents =
                await _context.InterviewerSchedules

                .Where(x =>
                    x.InterviewId ==
                        interview.Id &&

                    !x.IsDeleted)

                .ToListAsync();


            var currentUser =
                await _userManager
                    .GetUserAsync(User);


            var hrName =
                currentUser?.FullName
                ?? currentUser?.Email
                ?? "HR";


            foreach (var schedule
                     in calendarEvents)
            {
                schedule.StartDateTime =
                    newStart;


                schedule.EndDateTime =
                    newEnd;


                schedule.UpdatedBy =
                    hrName;


                schedule.UpdatedAt =
                    DateTime.Now;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Interview updated successfully.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    status =
                        interview.Status
                }
            );
        }


        // =========================================================
        // CANCEL INTERVIEW
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var interview =
                await _context.Interviews

                .Include(x =>
                    x.JobApplication)

                .FirstOrDefaultAsync(x =>
                    x.Id == id);


            if (interview == null)
            {
                return NotFound();
            }


            if (interview.Status == "Cancelled")
            {
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        status = "Cancelled"
                    }
                );
            }


            // =====================================================
            // CANCEL THE INTERVIEW
            // =====================================================

            interview.Status =
                "Cancelled";


            interview.FeedbackStatus =
                "Cancelled";


            // =====================================================
            // REMOVE INTERVIEWER CALENDAR EVENTS
            // =====================================================

            var calendarEvents =
                await _context.InterviewerSchedules

                .Where(x =>
                    x.InterviewId ==
                        interview.Id &&

                    !x.IsDeleted)

                .ToListAsync();


            var currentUser =
                await _userManager
                    .GetUserAsync(User);


            var hrName =
                currentUser?.FullName
                ?? currentUser?.Email
                ?? "HR";


            foreach (var schedule
                     in calendarEvents)
            {
                schedule.IsDeleted =
                    true;


                schedule.DeletedBy =
                    hrName;


                schedule.DeletedAt =
                    DateTime.Now;
            }


            // =====================================================
            // CANDIDATE OVERALL STATUS
            //
            // Only move back to Not Started when this was their
            // active scheduled interview and the candidate has not
            // already completed/failed another round.
            // =====================================================

            if (interview.JobApplication != null)
            {
                var application =
                    interview.JobApplication;


                if (
                    application.InterviewStageStatus ==
                        "Scheduled")
                {
                    application.InterviewStageStatus =
                        "Not Started";
                }
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Interview cancelled successfully.";


            // IMPORTANT:
            // Open Cancelled tab after redirect
            return RedirectToAction(
                nameof(Index),
                new
                {
                    status = "Cancelled"
                }
            );
        }


        // =========================================================
        // GET ELIGIBLE + AVAILABLE INTERVIEWERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetAvailableInterviewers(
            int jobId,
            DateTime date,
            TimeSpan time,
            int durationMinutes)
        {
            if (jobId <= 0)
            {
                return BadRequest(
                    "Invalid job."
                );
            }


            if (durationMinutes <= 0)
            {
                return BadRequest(
                    "Invalid interview duration."
                );
            }


            // =====================================================
            // FIND VACANCY
            // =====================================================

            var vacancy =
                await _context.JobVacancies
                    .FirstOrDefaultAsync(
                        x => x.Id == jobId
                    );


            if (vacancy == null)
            {
                return NotFound(
                    "Job vacancy not found."
                );
            }


            // =====================================================
            // BUILD INTERVIEW TIME RANGE
            // =====================================================

            var interviewStart =
                date.Date.Add(time);


            var interviewEnd =
                interviewStart.AddMinutes(
                    durationMinutes
                );


            // =====================================================
            // GET ALL INTERVIEWERS
            // =====================================================

            var interviewers =
                await _userManager
                    .GetUsersInRoleAsync(
                        "Interviewer"
                    );


            var availableInterviewers =
                new List<object>();


            // =====================================================
            // CHECK EACH INTERVIEWER
            // =====================================================

            foreach (var interviewer in interviewers)
            {
                // =================================================
                // 1. ELIGIBILITY
                // =================================================

                if (!IsEligibleInterviewer(
                    interviewer,
                    vacancy))
                {
                    continue;
                }


                // =================================================
                // 2. CALENDAR AVAILABILITY
                // =================================================

                var hasConflict =
    await _context
        .InterviewerSchedules
        .AnyAsync(schedule =>

            schedule.InterviewerId ==
                interviewer.Id

            &&

            !schedule.IsDeleted

            &&

            schedule.IsSystemGenerated

            &&

            schedule.ScheduleType == "Interview"

            &&

            schedule.InterviewId != null

            &&

            schedule.StartDateTime <
                interviewEnd

            &&

            schedule.EndDateTime >
                interviewStart
        );


                if (hasConflict)
                {
                    continue;
                }


                // =================================================
                // ELIGIBLE + AVAILABLE
                // =================================================

                availableInterviewers.Add(
                    new
                    {
                        id =
                            interviewer.Id,

                        name =
                            interviewer.FullName
                            ?? interviewer.Email
                            ?? "Interviewer",

                        email =
                            interviewer.Email
                            ?? "",

                        jobTitle =
                            interviewer.JobTitle
                            ?? "",

                        department =
                            interviewer.Department
                            ?? "",

                        seniority =
                            interviewer.SeniorityLevel
                            ?? "",

                        yearsOfExperience =
                            interviewer.YearsOfExperience,

                        skills =
                            interviewer.Skills
                            ?? ""
                    }
                );
            }


            return Json(
                availableInterviewers
            );
        }


        // =========================================================
        // INTERVIEWER ELIGIBILITY
        // =========================================================

        private bool IsEligibleInterviewer(
            ApplicationUser interviewer,
            JobVacancy vacancy)
        {
            // =====================================================
            // 1. PROFILE MUST BE COMPLETED
            // =====================================================

            if (!interviewer.InterviewerProfileCompleted)
            {
                return false;
            }


            // =====================================================
            // 2. DEPARTMENT MUST MATCH
            // =====================================================

            if (!DepartmentMatches(
                interviewer.Department,
                vacancy.Department))
            {
                return false;
            }


            // =====================================================
            // 3. DETERMINE VACANCY SENIORITY
            // =====================================================

            var vacancySeniority =
                GetVacancySeniority(
                    vacancy.JobTitle
                );


            // =====================================================
            // 4. INTERVIEWER SENIORITY
            // =====================================================

            var interviewerSeniority =
                GetInterviewerSeniorityRank(
                    interviewer.SeniorityLevel
                );


            // Interviewer must be same level
            // or above the vacancy level.
            if (interviewerSeniority <
                vacancySeniority)
            {
                return false;
            }


            // =====================================================
            // 5. EXPERIENCE CHECK
            // =====================================================

            var minimumExperience =
                GetMinimumInterviewerExperience(
                    vacancySeniority
                );


            if (interviewer.YearsOfExperience <
                minimumExperience)
            {
                return false;
            }


            // =====================================================
            // 6. JOB AREA RELEVANCE
            //
            // Avoid rejecting valid interviewers because of small
            // wording differences.
            //
            // We accept them when either:
            // - their title matches the vacancy field
            // OR
            // - their skills match the vacancy
            // =====================================================

            var titleMatches =
                JobAreaMatches(
                    interviewer.JobTitle,
                    vacancy.JobTitle
                );


            var skillsMatch =
                SkillsMatchVacancy(
                    interviewer.Skills,
                    vacancy
                );


            if (!titleMatches &&
                !skillsMatch)
            {
                return false;
            }


            return true;
        }


        // =========================================================
        // DEPARTMENT MATCH
        // =========================================================

        private bool DepartmentMatches(
            string? interviewerDepartment,
            string? vacancyDepartment)
        {
            // Vacancy has no department:
            // do not reject interviewer.
            if (string.IsNullOrWhiteSpace(
                vacancyDepartment))
            {
                return true;
            }


            if (string.IsNullOrWhiteSpace(
                interviewerDepartment))
            {
                return false;
            }


            var interviewerValue =
                NormalizeDepartment(
                    interviewerDepartment
                );


            var vacancyValue =
                NormalizeDepartment(
                    vacancyDepartment
                );


            return
                interviewerValue ==
                    vacancyValue

                ||

                interviewerValue.Contains(
                    vacancyValue
                )

                ||

                vacancyValue.Contains(
                    interviewerValue
                );
        }


        // =========================================================
        // NORMALIZE DEPARTMENT NAMES
        // =========================================================

        private string NormalizeDepartment(
            string value)
        {
            var text =
                value
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("&", "and");


            // AI
            if (
                text == "ai" ||
                text.Contains(
                    "artificial intelligence"
                )
            )
            {
                return "artificial intelligence";
            }


            // Machine Learning
            if (
                text == "ml" ||
                text.Contains(
                    "machine learning"
                )
            )
            {
                return "machine learning";
            }


            // IT
            if (
                text == "it" ||
                text.Contains(
                    "information technology"
                )
            )
            {
                return "information technology";
            }


            // HR
            if (
                text == "hr" ||
                text.Contains(
                    "human resources"
                )
            )
            {
                return "human resources";
            }


            // Data Science
            if (
                text == "ds" ||
                text.Contains(
                    "data science"
                )
            )
            {
                return "data science";
            }


            return text;
        }


        // =========================================================
        // VACANCY SENIORITY
        //
        // Rank:
        // 1 = Intern / Trainee
        // 2 = Junior
        // 3 = Mid / Normal
        // 4 = Senior
        // 5 = Lead / Principal / Manager
        // =========================================================

        private int GetVacancySeniority(
            string? jobTitle)
        {
            if (string.IsNullOrWhiteSpace(
                jobTitle))
            {
                return 3;
            }


            var title =
                jobTitle
                    .Trim()
                    .ToLowerInvariant();


            if (
                ContainsWord(
                    title,
                    "lead"
                )

                ||

                ContainsWord(
                    title,
                    "principal"
                )

                ||

                ContainsWord(
                    title,
                    "manager"
                )
            )
            {
                return 5;
            }


            if (
                ContainsWord(
                    title,
                    "senior"
                )

                ||

                ContainsWord(
                    title,
                    "sr"
                )
            )
            {
                return 4;
            }


            if (
                ContainsWord(
                    title,
                    "mid"
                )

                ||

                title.Contains(
                    "mid-level"
                )

                ||

                ContainsWord(
                    title,
                    "intermediate"
                )
            )
            {
                return 3;
            }


            if (
                ContainsWord(
                    title,
                    "junior"
                )

                ||

                ContainsWord(
                    title,
                    "jr"
                )
            )
            {
                return 2;
            }


            if (
                ContainsWord(
                    title,
                    "intern"
                )

                ||

                ContainsWord(
                    title,
                    "trainee"
                )
            )
            {
                return 1;
            }


            // Example:
            // AI Engineer
            //
            // If no level is stated,
            // treat it as mid-level for interviewer selection.
            return 3;
        }


        // =========================================================
        // INTERVIEWER SENIORITY
        // =========================================================

        private int GetInterviewerSeniorityRank(
            string? seniority)
        {
            if (string.IsNullOrWhiteSpace(
                seniority))
            {
                return 0;
            }


            var value =
                seniority
                    .Trim()
                    .ToLowerInvariant();


            return value switch
            {
                "intern" => 1,
                "trainee" => 1,

                "junior" => 2,
                "jr" => 2,
                "junior level" => 2,
                "junior-level" => 2,

                "mid" => 3,
                "mid level" => 3,
                "mid-level" => 3,
                "intermediate" => 3,

                "senior" => 4,
                "sr" => 4,
                "senior level" => 4,
                "senior-level" => 4,

                "lead" => 5,
                "principal" => 5,
                "manager" => 5,

                _ => 0
            };
        }


        // =========================================================
        // MINIMUM INTERVIEWER EXPERIENCE
        // =========================================================

        private int GetMinimumInterviewerExperience(
            int vacancySeniority)
        {
            return vacancySeniority switch
            {
                // Intern / Trainee vacancy
                1 => 1,

                // Junior vacancy
                2 => 2,

                // Normal / Mid vacancy
                3 => 3,

                // Senior vacancy
                4 => 5,

                // Lead / Principal / Manager vacancy
                5 => 7,

                _ => 3
            };
        }


        // =========================================================
        // JOB AREA MATCH
        // =========================================================

        private bool JobAreaMatches(
            string? interviewerJobTitle,
            string? vacancyJobTitle)
        {
            if (string.IsNullOrWhiteSpace(
                vacancyJobTitle))
            {
                return true;
            }


            if (string.IsNullOrWhiteSpace(
                interviewerJobTitle))
            {
                return false;
            }


            var interviewerKeywords =
                GetJobKeywords(
                    interviewerJobTitle
                );


            var vacancyKeywords =
                GetJobKeywords(
                    vacancyJobTitle
                );


            if (interviewerKeywords.Count == 0 ||
                vacancyKeywords.Count == 0)
            {
                return false;
            }


            return interviewerKeywords
                .Intersect(
                    vacancyKeywords,
                    StringComparer.OrdinalIgnoreCase
                )
                .Any();
        }


        // =========================================================
        // JOB KEYWORDS
        // =========================================================

        private List<string> GetJobKeywords(
            string text)
        {
            var ignoredWords =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "senior",
                    "junior",
                    "mid",
                    "lead",
                    "principal",
                    "manager",
                    "intern",
                    "trainee",

                    "engineer",
                    "developer",
                    "specialist",
                    "officer",
                    "executive",
                    "associate",

                    "sr",
                    "jr",

                    "and",
                    "or",
                    "the",
                    "for",
                    "of",
                    "with"
                };


            var words =
                text
                    .ToLowerInvariant()

                    .Split(
                        new[]
                        {
                            ' ',
                            '-',
                            '/',
                            ',',
                            '(',
                            ')',
                            '.'
                        },
                        StringSplitOptions.RemoveEmptyEntries
                    )

                    .Select(
                        x => x.Trim()
                    )

                    .Where(
                        x =>
                            x.Length >= 2 &&
                            !ignoredWords.Contains(x)
                    )

                    .Select(
                        NormalizeJobKeyword
                    )

                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )

                    .ToList();


            return words;
        }


        // =========================================================
        // NORMALIZE JOB KEYWORD
        // =========================================================

        private string NormalizeJobKeyword(
            string word)
        {
            var value =
                word
                    .Trim()
                    .ToLowerInvariant();


            return value switch
            {
                "ai" =>
                    "artificial-intelligence",

                "artificial" =>
                    "artificial-intelligence",

                "intelligence" =>
                    "artificial-intelligence",

                "ml" =>
                    "machine-learning",

                "machine" =>
                    "machine-learning",

                "learning" =>
                    "machine-learning",

                "dl" =>
                    "deep-learning",

                "deep" =>
                    "deep-learning",

                "data" =>
                    "data",

                "science" =>
                    "data",

                _ =>
                    value
            };
        }


        // =========================================================
        // SKILLS MATCH VACANCY
        // =========================================================

        private bool SkillsMatchVacancy(
            string? interviewerSkills,
            JobVacancy vacancy)
        {
            if (string.IsNullOrWhiteSpace(
                interviewerSkills))
            {
                return false;
            }


            var vacancyText =
                NormalizeMatchingText(
                    $"{vacancy.JobTitle} " +
                    $"{vacancy.Description} " +
                    $"{vacancy.Requirements} " +
                    $"{vacancy.PreferredRequirements}"
                );


            var skills =
                interviewerSkills

                    .Split(
                        new[]
                        {
                            ',',
                            ';',
                            '|'
                        },
                        StringSplitOptions.RemoveEmptyEntries
                    )

                    .Select(
                        x =>
                            NormalizeMatchingText(
                                x
                            )
                    )

                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x)
                    )

                    .ToList();


            return skills.Any(
                skill =>
                    skill.Length >= 2 &&
                    vacancyText.Contains(
                        skill
                    )
            );
        }


        // =========================================================
        // NORMALIZE MATCHING TEXT
        // =========================================================

        private string NormalizeMatchingText(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return "";
            }


            var text =
                value
                    .Trim()
                    .ToLowerInvariant();


            text =
                text.Replace(
                    "artificial intelligence",
                    "ai"
                );


            text =
                text.Replace(
                    "machine learning",
                    "ml"
                );


            text =
                text.Replace(
                    "deep learning",
                    "dl"
                );


            return text;
        }


        // =========================================================
        // SAFE WORD CHECK
        // =========================================================

        private bool ContainsWord(
            string text,
            string word)
        {
            return Regex.IsMatch(
                text,
                $@"\b{Regex.Escape(word)}\b",
                RegexOptions.IgnoreCase
            );
        }
    }
}
    
