using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class InterviewAssessment
    {
        public int Id { get; set; }


        // ============================================================
        // INTERVIEW
        // ============================================================

        [Required]
        public int InterviewId { get; set; }

        [ForeignKey(nameof(InterviewId))]
        public Interview? Interview { get; set; }


        // ============================================================
        // ASSESSMENT TEMPLATE
        // ============================================================

        [Required]
        public int AssessmentTemplateId { get; set; }

        [ForeignKey(nameof(AssessmentTemplateId))]
        public AssessmentTemplate? AssessmentTemplate { get; set; }


        // ============================================================
        // ASSIGNMENT
        // ============================================================

        public string AssignedBy { get; set; } = "";

        public DateTime AssignedDate { get; set; }
            = DateTime.Now;


        // Pending
        // In Progress
        // Submitted
        public string Status { get; set; }
            = "Pending";


        // ============================================================
        // RESULT ENTERED BY INTERVIEWER
        // ============================================================

        public double? Score { get; set; }


        public double? MaxScore { get; set; }


        // Pending
        // Pass
        // Fail
        public string ResultStatus { get; set; }
            = "Pending";


        public DateTime? AssessmentDate { get; set; }


        [StringLength(3000)]
        public string? Comments { get; set; }


        // ============================================================
        // WHO CONDUCTED IT
        // ============================================================

        public string? ConductedByInterviewerId { get; set; }

        [ForeignKey(nameof(ConductedByInterviewerId))]
        public ApplicationUser? ConductedByInterviewer { get; set; }


        // ============================================================
        // SUBMISSION
        // ============================================================

        public DateTime? SubmittedDate { get; set; }


        // ============================================================
        // ANSWERS
        // ============================================================

        public ICollection<AssessmentAnswer> Answers { get; set; }
            = new List<AssessmentAnswer>();
    }
}