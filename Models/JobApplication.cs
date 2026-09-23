using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class JobApplication
    {
        public int Id { get; set; }


        // ============================================================
        // CANDIDATE
        // ============================================================

        [Required]
        public string CandidateId { get; set; } = "";

        [ForeignKey(nameof(CandidateId))]
        public ApplicationUser? Candidate { get; set; }


        // ============================================================
        // VACANCY
        // ============================================================

        public int JobVacancyId { get; set; }

        [ForeignKey(nameof(JobVacancyId))]
        public JobVacancy? JobVacancy { get; set; }


        // ============================================================
        // APPLICATION STATUS
        //
        // Candidate-facing Sprint 1 status.
        // ============================================================

        public string Status { get; set; } = "Applied";


        // ============================================================
        // AI CV ANALYSIS
        // ============================================================

        public double AIScore { get; set; } = 0;

        public double AIRequiredScore { get; set; } = 0;

        public double AIPreferredScore { get; set; } = 0;

        public double AIOverallScore { get; set; } = 0;


        public string AIRecommendation { get; set; }
            = "Not Analyzed";


        public string AIRequiredResults { get; set; } = "[]";

        public string AIPreferredResults { get; set; } = "[]";


        public int AIRank { get; set; } = 0;


        // ============================================================
        // HR REVIEW
        // ============================================================

        public bool HRReviewed { get; set; } = false;

        public bool HRShortlisted { get; set; } = false;


        // ============================================================
        // HIRING MANAGER REVIEW
        // ============================================================

        public bool HiringManagerReviewed { get; set; } = false;

        public bool HiringManagerShortlisted { get; set; } = false;


        public string HiringManagerDecision { get; set; }
            = "Pending";


        public string HiringManagerComments { get; set; } = "";


        // ============================================================
        // INTERVIEW PIPELINE
        //
        // Not Started
        // Scheduled
        // In Progress
        // Completed
        // Failed
        // Cancelled
        // No Show
        // ============================================================

        public string InterviewStageStatus { get; set; }
            = "Not Started";


        // ============================================================
        // INTERVIEW PROCESS COMPLETED
        //
        // true when:
        // - final round passed
        // OR
        // - candidate failed and cannot continue
        // ============================================================

        public bool InterviewProcessCompleted { get; set; }
            = false;


        // ============================================================
        // OVERALL INTERVIEW SCORE
        //
        // We can use this later as a summary value.
        // It does NOT automatically decide Pass / Fail.
        // ============================================================

        public double InterviewOverallScore { get; set; }
            = 0;


        // ============================================================
        // FINAL INTERVIEW DECISION AUDIT
        // ============================================================

        public string? InterviewDecisionBy { get; set; }

        public DateTime? InterviewDecisionDate { get; set; }


        // ============================================================
        // CV / COVER LETTER
        // ============================================================

        public string? CVFilePath { get; set; }

        public string? CoverLetterFilePath { get; set; }


        // ============================================================
        // CANDIDATE DETAILS
        // ============================================================

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";


        // ============================================================
        // APPLICATION DATE
        // ============================================================

        public DateTime AppliedDate { get; set; }
            = DateTime.Now;
    }
}