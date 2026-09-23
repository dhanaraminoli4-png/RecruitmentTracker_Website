using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class AssessmentQuestion
    {
        public int Id { get; set; }


        // ============================================================
        // TEMPLATE
        // ============================================================

        [Required]
        public int AssessmentTemplateId { get; set; }

        [ForeignKey(nameof(AssessmentTemplateId))]
        public AssessmentTemplate? AssessmentTemplate { get; set; }


        // ============================================================
        // QUESTION / TASK
        // ============================================================

        [Required]
        [StringLength(2000)]
        public string QuestionText { get; set; } = "";


        // Examples:
        //
        // ShortText
        // LongText
        // Rating
        // YesNo
        // MultipleChoice
        // CodingTask
        // PracticalTask
        // Notes
        //
        [Required]
        public string QuestionType { get; set; }
            = "LongText";


        // Used for MultipleChoice etc.
        //
        // Store as JSON:
        // ["Option 1","Option 2","Option 3"]
        public string OptionsJson { get; set; }
            = "[]";


        public bool IsRequired { get; set; }
            = true;


        public int DisplayOrder { get; set; }
            = 1;


        // Optional marks for this question.
        //
        // The SYSTEM WILL NOT calculate the
        // candidate's overall assessment result.
        //
        // Interviewer can manually enter scores.
        public double? MaximumMarks { get; set; }
    }
}