using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class InterviewRoundController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewRoundController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // MAIN PAGE
        // =========================================================

        public async Task<IActionResult> Index(
            string? search,
            int? jobId,
            string status = "all",
            int? editId = null)
        {
            search = search?.Trim() ?? string.Empty;
            status = string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.ToLower();

            var query = _context.InterviewRounds
                .Include(r => r.JobVacancy)
                .AsNoTracking()
                .AsQueryable();


            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.Name.Contains(search) ||
                    (r.Description != null &&
                     r.Description.Contains(search)) ||
                    (r.JobVacancy != null &&
                     r.JobVacancy.JobTitle.Contains(search)));
            }


            // JOB FILTER
            if (jobId.HasValue)
            {
                query = query.Where(r =>
                    r.JobVacancyId == jobId.Value);
            }


            // Counts BEFORE status filtering
            var totalCount = await query.CountAsync();

            var activeCount = await query
                .CountAsync(r => r.IsActive);

            var inactiveCount = await query
                .CountAsync(r => !r.IsActive);


            // STATUS FILTER
            if (status == "active")
            {
                query = query.Where(r => r.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(r => !r.IsActive);
            }
            else
            {
                status = "all";
            }


            var rounds = await query
                .OrderBy(r => r.JobVacancy!.JobTitle)
                .ThenBy(r => r.SequenceNumber)
                .ToListAsync();


            var jobs = await _context.JobVacancies
    .AsNoTracking()
    .Where(j => j.IsActive)
    .OrderBy(j => j.JobCode)
    .ToListAsync();


            // Default form
            var form = new InterviewRound
            {
                IsActive = true,
                SequenceNumber = 1
            };


            // EDIT MODE
            if (editId.HasValue)
            {
                var existing = await _context.InterviewRounds
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.Id == editId.Value);

                if (existing != null)
                {
                    form = existing;
                }
            }


            var model = new InterviewRoundManagementViewModel
            {
                Form = form,
                InterviewRounds = rounds,
                Jobs = jobs,

                Search = search,
                JobId = jobId,
                Status = status,

                TotalCount = totalCount,
                ActiveCount = activeCount,
                InactiveCount = inactiveCount
            };


            return View(model);
        }


        // =========================================================
        // CREATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(Prefix = "Form")] InterviewRound form)
        {
            form.Name = form.Name?.Trim() ?? string.Empty;

            form.Description =
                string.IsNullOrWhiteSpace(form.Description)
                    ? null
                    : form.Description.Trim();


            if (string.IsNullOrWhiteSpace(form.Name))
            {
                TempData["Error"] =
                    "Interview round name is required.";

                return RedirectToAction(nameof(Index));
            }


            if (form.SequenceNumber < 1)
            {
                TempData["Error"] =
                    "Sequence number must be at least 1.";

                return RedirectToAction(nameof(Index));
            }


            var jobExists = await _context.JobVacancies
                .AnyAsync(j => j.Id == form.JobVacancyId);

            if (!jobExists)
            {
                TempData["Error"] =
                    "Please select a valid job vacancy.";

                return RedirectToAction(nameof(Index));
            }


            // A vacancy cannot have two Round 1s, two Round 2s, etc.
            var sequenceExists =
                await _context.InterviewRounds
                    .AnyAsync(r =>
                        r.JobVacancyId == form.JobVacancyId &&
                        r.SequenceNumber == form.SequenceNumber);

            if (sequenceExists)
            {
                TempData["Error"] =
                    $"This vacancy already has round {form.SequenceNumber}.";

                return RedirectToAction(nameof(Index));
            }


            var interviewRound = new InterviewRound
            {
                JobVacancyId = form.JobVacancyId,

                Name = form.Name,

                SequenceNumber = form.SequenceNumber,

                Description = form.Description,

                IsActive = form.IsActive,

                CreatedDate = DateTime.Now
            };


            _context.InterviewRounds.Add(interviewRound);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Interview round added successfully.";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // UPDATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            [Bind(Prefix = "Form")] InterviewRound form)
        {
            var existing = await _context.InterviewRounds
                .FirstOrDefaultAsync(r =>
                    r.Id == form.Id);

            if (existing == null)
            {
                return NotFound();
            }


            form.Name = form.Name?.Trim() ?? string.Empty;

            form.Description =
                string.IsNullOrWhiteSpace(form.Description)
                    ? null
                    : form.Description.Trim();


            if (string.IsNullOrWhiteSpace(form.Name))
            {
                TempData["Error"] =
                    "Interview round name is required.";

                return RedirectToAction(
                    nameof(Index),
                    new { editId = form.Id });
            }


            var sequenceExists =
                await _context.InterviewRounds
                    .AnyAsync(r =>
                        r.Id != form.Id &&
                        r.JobVacancyId == form.JobVacancyId &&
                        r.SequenceNumber == form.SequenceNumber);

            if (sequenceExists)
            {
                TempData["Error"] =
                    $"This vacancy already has round {form.SequenceNumber}.";

                return RedirectToAction(
                    nameof(Index),
                    new { editId = form.Id });
            }


            existing.JobVacancyId =
                form.JobVacancyId;

            existing.Name =
                form.Name;

            existing.SequenceNumber =
                form.SequenceNumber;

            existing.Description =
                form.Description;

            existing.IsActive =
                form.IsActive;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Interview round updated successfully.";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ACTIVE / INACTIVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var round =
                await _context.InterviewRounds.FindAsync(id);

            if (round == null)
            {
                return NotFound();
            }


            round.IsActive = !round.IsActive;

            await _context.SaveChangesAsync();


            TempData["Success"] =
                round.IsActive
                    ? "Interview round activated."
                    : "Interview round deactivated.";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var round =
                await _context.InterviewRounds.FindAsync(id);

            if (round == null)
            {
                return NotFound();
            }


            _context.InterviewRounds.Remove(round);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Interview round deleted.";


            return RedirectToAction(nameof(Index));
        }
    }
}