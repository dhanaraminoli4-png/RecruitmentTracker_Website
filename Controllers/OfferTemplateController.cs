using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;

namespace RecruitmentTracker.Controllers
{
    [Authorize(Roles = "HR")]
    public class OfferTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfferTemplateController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var templates = await _context.OfferTemplates
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(templates);
        }


        // =========================================================
        // VIEW TEMPLATE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var template = await _context.OfferTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            return View(template);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new OfferTemplate());
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfferTemplate model)
        {
            // ==========================================
            // VALIDATE NAME
            // ==========================================

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Template name is required.");
            }


            // ==========================================
            // VALIDATE CONTENT
            // ==========================================

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(
                    nameof(model.Content),
                    "Template content is required.");
            }


            // ==========================================
            // DETECT VARIABLES
            // ==========================================

            model.Variables = ExtractVariables(model.Content);


            if (string.IsNullOrWhiteSpace(model.Variables))
            {
                ModelState.AddModelError(
                    nameof(model.Content),
                    "Add at least one variable such as {{candidate_name}}.");
            }


            // ==========================================
            // IF INVALID -> SHOW CREATE PAGE AGAIN
            // ==========================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ==========================================
            // CREATE NEW DATABASE ENTITY
            // ==========================================

            var template = new OfferTemplate
            {
                Name = model.Name.Trim(),

                Content = model.Content,

                Variables = model.Variables,

                IsActive = model.IsActive,

                CreatedDate = DateTime.Now
            };


            // ==========================================
            // SAVE TO DATABASE
            // ==========================================

            _context.OfferTemplates.Add(template);

            await _context.SaveChangesAsync();


            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["Success"] =
                "Offer template created successfully.";


            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.OfferTemplates
                .FirstOrDefaultAsync(x => x.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            _context.OfferTemplates.Remove(template);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Offer template deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template =
                await _context.OfferTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            return View(template);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            OfferTemplate model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var template =
                await _context.OfferTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Template name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(
                    nameof(model.Content),
                    "Template content is required.");
            }

            model.Variables =
                ExtractVariables(model.Content);

            if (string.IsNullOrWhiteSpace(model.Variables))
            {
                ModelState.AddModelError(
                    nameof(model.Content),
                    "Add at least one variable such as {{candidate_name}}.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            template.Name = model.Name;
            template.Content = model.Content;
            template.Variables = model.Variables;
            template.IsActive = model.IsActive;
            template.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Offer template updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET TEMPLATE FOR PREVIEW / GENERATE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetTemplate(int id)
        {
            var template = await _context.OfferTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            var variables = template.Variables
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Json(new
            {
                id = template.Id,
                name = template.Name,
                content = template.Content,
                isActive = template.IsActive,
                createdDate = template.CreatedDate.ToString("yyyy-MM-dd"),
                variables
            });
        }


        

        // =========================================================
        // GENERATE OFFER -> PREVIEW
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            int templateId,
            Dictionary<string, string> values)
        {
            var template = await _context.OfferTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == templateId &&
                    x.IsActive);

            if (template == null)
            {
                return NotFound();
            }

            var finalContent = template.Content;


            // Replace every {{variable}}
            foreach (var item in values)
            {
                finalContent = Regex.Replace(
                    finalContent,
                    @"\{\{\s*" +
                    Regex.Escape(item.Key) +
                    @"\s*\}\}",
                    item.Value ?? "",
                    RegexOptions.IgnoreCase);
            }


            var viewModel =
                new RecruitmentTracker.ViewModels.OfferPreviewViewModel
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    FinalContent = finalContent
                };


            return View(
                "OfferPreview",
                viewModel);
        }

        // =========================================================
        // DOWNLOAD OFFER AS PDF
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadPdf(
            string templateName,
            string finalContent)
        {
            if (string.IsNullOrWhiteSpace(finalContent))
            {
                return BadRequest();
            }


            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);

                    page.DefaultTextStyle(
                        x => x.FontSize(11));


                    // =============================================
                    // HEADER
                    // =============================================

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("HIRETRACK")
                                .Bold()
                                .FontSize(22);

                            column.Item()
                                .PaddingTop(4)
                                .Text("OFFICIAL OFFER LETTER")
                                .FontSize(10);

                            column.Item()
                                .PaddingTop(15)
                                .LineHorizontal(1);
                        });


                    // =============================================
                    // CONTENT
                    // =============================================

                    page.Content()
                        .PaddingVertical(30)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item()
                                .Text(DateTime.Now.ToString(
                                    "MMMM dd, yyyy"))
                                .FontSize(10);


                            column.Item()
                                .Text(finalContent)
                                .FontSize(11)
                                .LineHeight(1.5f);


                            column.Item()
                                .PaddingTop(30)
                                .Text("Sincerely,")
                                .FontSize(11);


                            column.Item()
                                .PaddingTop(20)
                                .Text("Human Resources")
                                .Bold();


                            column.Item()
                                .Text("HireTrack");
                        });


                    // =============================================
                    // FOOTER
                    // =============================================

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated through HireTrack • ");

                            text.CurrentPageNumber();

                            text.Span(" / ");

                            text.TotalPages();
                        });
                });

            }).GeneratePdf();


            var safeName =
                string.IsNullOrWhiteSpace(templateName)
                    ? "Offer_Letter"
                    : Regex.Replace(
                        templateName,
                        @"[^a-zA-Z0-9_-]",
                        "_");


            return File(
                pdfBytes,
                "application/pdf",
                $"{safeName}.pdf");
        }


        // =========================================================
        // EXTRACT {{VARIABLES}}
        // =========================================================

        private static string ExtractVariables(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var matches = Regex.Matches(
                content,
                @"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}");

            var variables = matches
                .Select(x => x.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return string.Join(",", variables);
        }
    }
}