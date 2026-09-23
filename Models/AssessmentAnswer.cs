using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class AssessmentAnswer
    {
        public int Id { get; set; }


        // ============================================================
        // ASSIGNED ASSESSMENT
        // ============================================================

        [Required]
        public int InterviewAssessmentId { get; set; }

        [ForeignKey(nameof(InterviewAssessmentId))]
        public InterviewAssessment? InterviewAssessment { get; set; }


        // ============================================================
        // QUESTION
        // ============================================================

        [Required]
        public int AssessmentQuestionId { get; set; }

        [ForeignKey(nameof(AssessmentQuestionId))]
        public AssessmentQuestion? AssessmentQuestion { get; set; }


        // ============================================================
        // INTERVIEWER RESPONSE
        // ============================================================

        public string? AnswerText { get; set; }


        // Optional manually entered marks.
        public double? Score { get; set; }
    }
}