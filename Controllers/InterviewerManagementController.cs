using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class InterviewerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewerManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // INTERVIEWER LIST
        // =========================================================

        public async Task<IActionResult> Index(string? search)
        {
            var interviewers =
                await _userManager.GetUsersInRoleAsync("Interviewer");

            var result =
                interviewers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term =
                    search.Trim().ToLowerInvariant();

                result =
                    result.Where(x =>
                        (x.FullName ?? "")
                            .ToLowerInvariant()
                            .Contains(term)

                        ||

                        (x.Email ?? "")
                            .ToLowerInvariant()
                            .Contains(term)

                        ||

                        (x.JobTitle ?? "")
                            .ToLowerInvariant()
                            .Contains(term)

                        ||

                        (x.Department ?? "")
                            .ToLowerInvariant()
                            .Contains(term));
            }

            ViewBag.Search =
                search;

            return View(
                result
                    .OrderBy(x => x.FullName)
                    .ToList());
        }


        // =========================================================
        // SHARED INTERVIEWER CALENDAR
        // =========================================================

        public async Task<IActionResult> Calendar(string id)
        {
            var interviewer =
                await _userManager.FindByIdAsync(id);

            if (interviewer == null)
            {
                return NotFound();
            }

            var isInterviewer =
                await _userManager.IsInRoleAsync(
                    interviewer,
                    "Interviewer");

            if (!isInterviewer)
            {
                return NotFound();
            }

            ViewBag.InterviewerId =
                interviewer.Id;

            ViewBag.InterviewerName =
                interviewer.FullName
                ?? interviewer.Email
                ?? "Interviewer";

            ViewBag.Email =
                interviewer.Email ?? "";

            ViewBag.JobTitle =
                interviewer.JobTitle ?? "";

            ViewBag.Department =
                interviewer.Department ?? "";

            ViewBag.Seniority =
                interviewer.SeniorityLevel ?? "";

            ViewBag.Experience =
                interviewer.YearsOfExperience;

            return View();
        }


        // =========================================================
        // GET SHARED CALENDAR EVENTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetEvents(
            string interviewerId,
            DateTime start,
            DateTime end)
        {
            var events =
                await _context.InterviewerSchedules

                    .Where(x =>
                        x.InterviewerId == interviewerId &&
                        !x.IsDeleted &&
                        x.StartDateTime < end &&
                        x.EndDateTime > start)

                    .OrderBy(x =>
                        x.StartDateTime)

                    .Select(x => new
                    {
                        id =
                            x.Id,

                        title =
                            x.Title ?? x.ScheduleType,

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

            return Json(events);
        }


        // =========================================================
        // HR ADD EVENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(
            string interviewerId,
            DateTime start,
            DateTime end,
            string scheduleType,
            string? title,
            string? notes,
            bool isAllDay = false)
        {
            if (end <= start)
            {
                return BadRequest(
                    "End time must be after start time.");
            }

            var interviewer =
                await _userManager.FindByIdAsync(
                    interviewerId);

            if (interviewer == null)
            {
                return NotFound();
            }

            var isInterviewer =
                await _userManager.IsInRoleAsync(
                    interviewer,
                    "Interviewer");

            if (!isInterviewer)
            {
                return BadRequest(
                    "Selected user is not an interviewer.");
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

            if (!allowedTypes.Contains(scheduleType))
            {
                return BadRequest(
                    "Invalid event type.");
            }

            var hr =
                await _userManager.GetUserAsync(User);

            var schedule =
                new InterviewerSchedule
                {
                    InterviewerId =
                        interviewerId,

                    StartDateTime =
                        start,

                    EndDateTime =
                        end,

                    IsAllDay =
                        isAllDay,

                    ScheduleType =
                        scheduleType,

                    Title =
                        string.IsNullOrWhiteSpace(title)
                            ? scheduleType
                            : title.Trim(),

                    Notes =
                        string.IsNullOrWhiteSpace(notes)
                            ? null
                            : notes.Trim(),

                    IsSystemGenerated =
                        false,

                    IsDeleted =
                        false,

                    CreatedBy =
                        hr?.FullName
                        ?? hr?.Email
                        ?? "HR",

                    CreatedAt =
                        DateTime.Now
                };

            _context.InterviewerSchedules
                .Add(schedule);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }


        // =========================================================
        // HR UPDATE EVENT
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
            var schedule =
                await _context.InterviewerSchedules

                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        !x.IsDeleted);

            if (schedule == null)
            {
                return NotFound();
            }

            if (schedule.IsSystemGenerated)
            {
                return BadRequest(
                    "Scheduled interview events cannot be edited here.");
            }

            if (end <= start)
            {
                return BadRequest(
                    "End time must be after start time.");
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

            if (!allowedTypes.Contains(scheduleType))
            {
                return BadRequest(
                    "Invalid event type.");
            }

            var hr =
                await _userManager.GetUserAsync(User);

            schedule.StartDateTime =
                start;

            schedule.EndDateTime =
                end;

            schedule.ScheduleType =
                scheduleType;

            schedule.Title =
                string.IsNullOrWhiteSpace(title)
                    ? scheduleType
                    : title.Trim();

            schedule.Notes =
                string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim();

            schedule.IsAllDay =
                isAllDay;

            schedule.UpdatedBy =
                hr?.FullName
                ?? hr?.Email
                ?? "HR";

            schedule.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }


        // =========================================================
        // HR DELETE EVENT
        // SOFT DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var schedule =
                await _context.InterviewerSchedules

                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        !x.IsDeleted);

            if (schedule == null)
            {
                return NotFound();
            }

            if (schedule.IsSystemGenerated)
            {
                return BadRequest(
                    "Scheduled interviews cannot be deleted from this calendar.");
            }

            var hr =
                await _userManager.GetUserAsync(User);

            schedule.IsDeleted =
                true;

            schedule.DeletedBy =
                hr?.FullName
                ?? hr?.Email
                ?? "HR";

            schedule.DeletedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }
    }
}