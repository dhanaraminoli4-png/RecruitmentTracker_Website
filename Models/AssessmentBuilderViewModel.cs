using Microsoft.AspNetCore.Mvc.Rendering;

namespace RecruitmentTracker.Models
{
    public class AssessmentBuilderViewModel
    {
        // ============================================================
        // ASSESSMENT
        // ============================================================

        public AssessmentTemplate Assessment { get; set; }
            = new AssessmentTemplate();


        // ============================================================
        // QUESTIONS
        // ============================================================

        public List<AssessmentQuestion> Questions { get; set; }
            = new List<AssessmentQuestion>();


        // ============================================================
        // DROPDOWNS
        // ============================================================

        public List<SelectListItem> Jobs { get; set; }
            = new List<SelectListItem>();


        public List<SelectListItem> Rounds { get; set; }
            = new List<SelectListItem>();
    }
}