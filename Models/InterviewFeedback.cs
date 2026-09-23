using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class InterviewFeedback
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
        // INTERVIEWER
        // ============================================================

        [Required]
        public string InterviewerId { get; set; } = "";

        [ForeignKey(nameof(InterviewerId))]
        public ApplicationUser? Interviewer { get; set; }


        // ============================================================
        // RATINGS
        //
        // These are manually entered by interviewer.
        // System does NOT calculate OverallRating.
        // ============================================================

        [Range(1, 5)]
        public int TechnicalRating { get; set; }


        [Range(1, 5)]
        public int ProblemSolvingRating { get; set; }


        [Range(1, 5)]
        public int CommunicationRating { get; set; }


        [Range(1, 5)]
        public int RoleFitRating { get; set; }


        [Range(1, 5)]
        public int ProfessionalismRating { get; set; }


        [Range(1, 5)]
        public int OverallRating { get; set; }


        // ============================================================
        // WRITTEN FEEDBACK
        // ============================================================

        [StringLength(2000)]
        public string Strengths { get; set; } = "";


        [StringLength(2000)]
        public string Weaknesses { get; set; } = "";


        [StringLength(4000)]
        public string Comments { get; set; } = "";


        // ============================================================
        // RECOMMENDATION
        //
        // Strong Pass
        // Pass
        // Borderline
        // Fail
        // ============================================================

        public string Recommendation { get; set; }
            = "Pending";


        // ============================================================
        // SUBMITTED
        // ============================================================

        public DateTime SubmittedDate { get; set; }
            = DateTime.Now;
    }
}