using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using RecruitmentTracker.Data;
using RecruitmentTracker.Models;


namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class AssessmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;


        public AssessmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }


        // ============================================================
        // ASSESSMENT LIST
        // ============================================================


        // ============================================================
        // ASSESSMENT LIST
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            int? jobId = null)
        {
            var query =
                _context.Set<AssessmentTemplate>()

                    .Include(x => x.JobVacancy)

                    .Include(x => x.InterviewRound)

                    .Include(x => x.Questions)

                    .AsNoTracking()

                    .AsQueryable();


            // ========================================================
            // JOB FILTER
            // ========================================================

            if (jobId.HasValue)
            {
                query =
                    query.Where(x =>
                        x.JobVacancyId == jobId.Value);
            }


            // ========================================================
            // SEARCH
            // ========================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value =
                    search.Trim();


                query =
                    query.Where(x =>

                        x.Name.Contains(value)

                        ||

                        (
                            x.JobVacancy != null &&
                            x.JobVacancy.JobTitle.Contains(value)
                        )

                        ||

                        (
                            x.InterviewRound != null &&
                            x.InterviewRound.Name.Contains(value)
                        )
                    );
            }


            var assessments =
                await query

                    .OrderBy(x =>
                        x.JobVacancy!.JobTitle)

                    .ThenBy(x =>
                        x.InterviewRound!.SequenceNumber)

                    .ThenBy(x =>
                        x.Name)

                    .ToListAsync();


            ViewBag.Search =
                search;


            ViewBag.JobId =
                jobId;


            ViewBag.Jobs =
                await _context.JobVacancies

                    .OrderBy(x =>
                        x.JobTitle)

                    .Select(x =>
                        new SelectListItem
                        {
                            Value =
                                x.Id.ToString(),

                            Text =
                                x.JobTitle
                        })

                    .ToListAsync();


            return View(
                assessments
            );
        }


        // ============================================================
        // CREATE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model =
                new AssessmentBuilderViewModel();


            await PopulateDropdowns(
                model
            );


            return View(
                model
            );
        }


        // ============================================================
        // GET ROUNDS FOR SELECTED JOB
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetRounds(
            int jobId)
        {
            var rounds =
                await _context.InterviewRounds

                    .Where(x =>
                        x.JobVacancyId == jobId &&
                        x.IsActive)

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


            return Json(
                rounds
            );
        }


        // ============================================================
        // CREATE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AssessmentBuilderViewModel model)
        {
            // ========================================================
            // BASIC VALIDATION
            // ========================================================

            if (model.Assessment.JobVacancyId <= 0)
            {
                ModelState.AddModelError(
                    "Assessment.JobVacancyId",
                    "Please select a vacancy."
                );
            }


            if (model.Assessment.InterviewRoundId <= 0)
            {
                ModelState.AddModelError(
                    "Assessment.InterviewRoundId",
                    "Please select an interview round."
                );
            }


            if (string.IsNullOrWhiteSpace(
                model.Assessment.Name))
            {
                ModelState.AddModelError(
                    "Assessment.Name",
                    "Assessment name is required."
                );
            }


            // ========================================================
            // VERIFY ROUND BELONGS TO VACANCY
            // ========================================================

            var validRound =
                await _context.InterviewRounds

                    .AnyAsync(x =>

                        x.Id ==
                            model.Assessment.InterviewRoundId

                        &&

                        x.JobVacancyId ==
                            model.Assessment.JobVacancyId
                    );


            if (!validRound)
            {
                ModelState.AddModelError(
                    "Assessment.InterviewRoundId",
                    "The selected round does not belong to this vacancy."
                );
            }


            // ========================================================
            // MUST HAVE AT LEAST ONE QUESTION
            // ========================================================

            model.Questions =
                model.Questions
                    ?.Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.QuestionText))
                    .ToList()

                ?? new List<AssessmentQuestion>();


            if (model.Questions.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one question or task."
                );
            }


            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(
                    model
                );


                return View(
                    model
                );
            }


            // ========================================================
            // LOGGED-IN HR
            // ========================================================

            var user =
                await _userManager
                    .GetUserAsync(User);


            // ========================================================
            // CREATE ASSESSMENT
            // ========================================================

            var assessment =
                new AssessmentTemplate
                {
                    JobVacancyId =
                        model.Assessment.JobVacancyId,

                    InterviewRoundId =
                        model.Assessment.InterviewRoundId,

                    Name =
                        model.Assessment.Name.Trim(),

                    Description =
                        string.IsNullOrWhiteSpace(
                            model.Assessment.Description)

                            ? null

                            : model.Assessment.Description.Trim(),

                    Instructions =
                        string.IsNullOrWhiteSpace(
                            model.Assessment.Instructions)

                            ? null

                            : model.Assessment.Instructions.Trim(),

                    IsActive =
                        model.Assessment.IsActive,

                    CreatedBy =
                        user?.FullName
                        ?? user?.Email
                        ?? "HR",

                    CreatedDate =
                        DateTime.Now
                };


            _context.Set<AssessmentTemplate>().Add(
                assessment
            );


            await _context.SaveChangesAsync();


            // ========================================================
            // CREATE QUESTIONS
            // ========================================================

            var order = 1;


            foreach (var item in model.Questions)
            {
                var question =
                    new AssessmentQuestion
                    {
                        AssessmentTemplateId =
                            assessment.Id,

                        QuestionText =
                            item.QuestionText.Trim(),

                        QuestionType =
                            string.IsNullOrWhiteSpace(
                                item.QuestionType)

                                ? "LongText"

                                : item.QuestionType,

                        OptionsJson =
                            string.IsNullOrWhiteSpace(
                                item.OptionsJson)

                                ? "[]"

                                : item.OptionsJson,

                        IsRequired =
                            item.IsRequired,

                        MaximumMarks =
                            item.MaximumMarks,

                        DisplayOrder =
                            order
                    };


                _context.Set<AssessmentQuestion>().Add(
                    question
                );


                order++;
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Assessment created successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // EDIT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var assessment =
                await _context.Set<AssessmentTemplate>()

                    .Include(x =>
                        x.Questions)

                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (assessment == null)
            {
                return NotFound();
            }


            var model =
                new AssessmentBuilderViewModel
                {
                    Assessment =
                        assessment,

                    Questions =
                        assessment.Questions

                            .OrderBy(x =>
                                x.DisplayOrder)

                            .ToList()
                };


            await PopulateDropdowns(
                model
            );


            return View(
                "Create",
                model
            );
        }


        // ============================================================
        // EDIT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AssessmentBuilderViewModel model)
        {
            var existing =
                await _context.Set<AssessmentTemplate>()

                    .Include(x =>
                        x.Questions)

                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            model.Assessment.Id);


            if (existing == null)
            {
                return NotFound();
            }


            model.Questions =
                model.Questions

                    ?.Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.QuestionText))

                    .ToList()

                ?? new List<AssessmentQuestion>();


            if (string.IsNullOrWhiteSpace(
                model.Assessment.Name))
            {
                ModelState.AddModelError(
                    "Assessment.Name",
                    "Assessment name is required."
                );
            }


            if (model.Questions.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one question or task."
                );
            }


            var validRound =
                await _context.InterviewRounds

                    .AnyAsync(x =>

                        x.Id ==
                            model.Assessment.InterviewRoundId

                        &&

                        x.JobVacancyId ==
                            model.Assessment.JobVacancyId
                    );


            if (!validRound)
            {
                ModelState.AddModelError(
                    "Assessment.InterviewRoundId",
                    "Invalid interview round."
                );
            }


            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(
                    model
                );


                return View(
                    "Create",
                    model
                );
            }


            var user =
                await _userManager
                    .GetUserAsync(User);


            // ========================================================
            // UPDATE ASSESSMENT
            // ========================================================

            existing.JobVacancyId =
                model.Assessment.JobVacancyId;


            existing.InterviewRoundId =
                model.Assessment.InterviewRoundId;


            existing.Name =
                model.Assessment.Name.Trim();


            existing.Description =
                string.IsNullOrWhiteSpace(
                    model.Assessment.Description)

                    ? null

                    : model.Assessment.Description.Trim();


            existing.Instructions =
                string.IsNullOrWhiteSpace(
                    model.Assessment.Instructions)

                    ? null

                    : model.Assessment.Instructions.Trim();


            existing.IsActive =
                model.Assessment.IsActive;


            existing.UpdatedBy =
                user?.FullName
                ?? user?.Email
                ?? "HR";


            existing.UpdatedDate =
                DateTime.Now;


            // ========================================================
            // REPLACE QUESTIONS
            // ========================================================

            _context.Set<AssessmentQuestion>().RemoveRange(
                existing.Questions
            );


            var order = 1;


            foreach (var item in model.Questions)
            {
                _context.Set<AssessmentQuestion>().Add(
                    new AssessmentQuestion
                    {
                        AssessmentTemplateId =
                            existing.Id,

                        QuestionText =
                            item.QuestionText.Trim(),

                        QuestionType =
                            string.IsNullOrWhiteSpace(
                                item.QuestionType)

                                ? "LongText"

                                : item.QuestionType,

                        OptionsJson =
                            string.IsNullOrWhiteSpace(
                                item.OptionsJson)

                                ? "[]"

                                : item.OptionsJson,

                        IsRequired =
                            item.IsRequired,

                        MaximumMarks =
                            item.MaximumMarks,

                        DisplayOrder =
                            order++
                    }
                );
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Assessment updated successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // CHANGE ACTIVE STATUS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id)
        {
            var assessment =
                await _context.Set<AssessmentTemplate>()
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (assessment == null)
            {
                return NotFound();
            }


            assessment.IsActive =
                !assessment.IsActive;


            var user =
                await _userManager
                    .GetUserAsync(User);


            assessment.UpdatedBy =
                user?.FullName
                ?? user?.Email
                ?? "HR";


            assessment.UpdatedDate =
                DateTime.Now;


            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // DELETE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var assessment =
                await _context.Set<AssessmentTemplate>()

                    .Include(x =>
                        x.Questions)

                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (assessment == null)
            {
                return NotFound();
            }


            // Prevent deletion if already assigned
            var alreadyUsed =
                await _context.Set<InterviewAssessment>()

                    .AnyAsync(x =>
                        x.AssessmentTemplateId == id);


            if (alreadyUsed)
            {
                TempData["Error"] =
                    "This assessment has already been assigned to an interview. Deactivate it instead of deleting it.";


                return RedirectToAction(
                    nameof(Index)
                );
            }


            _context.Set<AssessmentQuestion>().RemoveRange(
                assessment.Questions
            );


            _context.Set<AssessmentTemplate>().Remove(
                assessment
            );


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Assessment deleted successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ============================================================
        // POPULATE DROPDOWNS
        // ============================================================

        private async Task PopulateDropdowns(
            AssessmentBuilderViewModel model)
        {
            model.Jobs =
                await _context.JobVacancies

                    .Where(x =>
                        x.IsActive)

                    .OrderBy(x =>
                        x.JobTitle)

                    .Select(x =>
                        new SelectListItem
                        {
                            Value =
                                x.Id.ToString(),

                            Text =
                                x.JobTitle
                        })

                    .ToListAsync();


            if (model.Assessment.JobVacancyId > 0)
            {
                model.Rounds =
                    await _context.InterviewRounds

                        .Where(x =>

                            x.JobVacancyId ==
                                model.Assessment.JobVacancyId

                            &&

                            x.IsActive
                        )

                        .OrderBy(x =>
                            x.SequenceNumber)

                        .Select(x =>
                            new SelectListItem
                            {
                                Value =
                                    x.Id.ToString(),

                                Text =
                                    $"Round {x.SequenceNumber} — {x.Name}"
                            })

                        .ToListAsync();
            }
            else
            {
                model.Rounds =
                    new List<SelectListItem>();
            }
        }
    }
}