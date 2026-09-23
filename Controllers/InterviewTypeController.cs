using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class InterviewTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InterviewTypeController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // INTERVIEW TYPE MANAGEMENT PAGE
        // ============================================================

        public async Task<IActionResult> Index(
            string? search,
            string status = "all",
            int? editId = null)
        {
            status = string.IsNullOrWhiteSpace(status)
                ? "all"
                : status.Trim().ToLowerInvariant();

            var query = _context.InterviewTypes
                .AsNoTracking()
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(t =>
                    t.Name.Contains(term) ||
                    (
                        t.Description != null &&
                        t.Description.Contains(term)
                    ));
            }

            // STATUS FILTER
            if (status == "active")
            {
                query = query.Where(t => t.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(t => !t.IsActive);
            }
            else
            {
                status = "all";
            }

            var interviewTypes = await query
                .OrderByDescending(t => t.IsActive)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // DEFAULT FORM
            var form = new InterviewType
            {
                IsActive = true
            };

            // EDIT MODE
            if (editId.HasValue)
            {
                var existing =
                    await _context.InterviewTypes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            t => t.Id == editId.Value);

                if (existing != null)
                {
                    form = existing;
                }
            }

            var model =
                new InterviewTypeManagementViewModel
                {
                    Form = form,

                    InterviewTypes = interviewTypes,

                    Search = search ?? string.Empty,

                    Status = status
                };

            return View(model);
        }

        // ============================================================
        // CREATE INTERVIEW TYPE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(Prefix = "Form")]
            InterviewType form)
        {
            form.Name =
                form.Name?.Trim()
                ?? string.Empty;

            form.Description =
                string.IsNullOrWhiteSpace(
                    form.Description)
                    ? null
                    : form.Description.Trim();

            if (string.IsNullOrWhiteSpace(
                form.Name))
            {
                TempData["Error"] =
                    "Interview type name is required.";

                return RedirectToAction(
                    nameof(Index));
            }

            // DO NOT ALLOW DUPLICATES
            var duplicateExists =
                await _context.InterviewTypes
                    .AnyAsync(t =>
                        t.Name == form.Name);

            if (duplicateExists)
            {
                TempData["Error"] =
                    "An interview type with this name already exists.";

                return RedirectToAction(
                    nameof(Index));
            }

            var interviewType =
                new InterviewType
                {
                    Name = form.Name,

                    Description =
                        form.Description,

                    IsActive =
                        form.IsActive,

                    CreatedDate =
                        DateTime.Now
                };

            _context.InterviewTypes
                .Add(interviewType);

            await _context
                .SaveChangesAsync();

            TempData["Success"] =
                "Interview type added successfully.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // UPDATE INTERVIEW TYPE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            [Bind(Prefix = "Form")]
            InterviewType form)
        {
            var existing =
                await _context.InterviewTypes
                    .FirstOrDefaultAsync(
                        t => t.Id == form.Id);

            if (existing == null)
            {
                return NotFound();
            }

            form.Name =
                form.Name?.Trim()
                ?? string.Empty;

            form.Description =
                string.IsNullOrWhiteSpace(
                    form.Description)
                    ? null
                    : form.Description.Trim();

            if (string.IsNullOrWhiteSpace(
                form.Name))
            {
                TempData["Error"] =
                    "Interview type name is required.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        editId = form.Id
                    });
            }

            var duplicateExists =
                await _context.InterviewTypes
                    .AnyAsync(t =>
                        t.Id != form.Id &&
                        t.Name == form.Name);

            if (duplicateExists)
            {
                TempData["Error"] =
                    "Another interview type already uses this name.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        editId = form.Id
                    });
            }

            existing.Name =
                form.Name;

            existing.Description =
                form.Description;

            existing.IsActive =
                form.IsActive;

            await _context
                .SaveChangesAsync();

            TempData["Success"] =
                "Interview type updated successfully.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // ACTIVATE / DEACTIVATE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id)
        {
            var interviewType =
                await _context.InterviewTypes
                    .FindAsync(id);

            if (interviewType == null)
            {
                return NotFound();
            }

            interviewType.IsActive =
                !interviewType.IsActive;

            await _context
                .SaveChangesAsync();

            TempData["Success"] =
                interviewType.IsActive
                    ? "Interview type activated."
                    : "Interview type deactivated.";

            return RedirectToAction(
                nameof(Index));
        }

        // ============================================================
        // DELETE
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var interviewType =
                await _context.InterviewTypes
                    .FindAsync(id);

            if (interviewType == null)
            {
                return NotFound();
            }

            _context.InterviewTypes
                .Remove(interviewType);

            await _context
                .SaveChangesAsync();

            TempData["Success"] =
                "Interview type deleted.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}