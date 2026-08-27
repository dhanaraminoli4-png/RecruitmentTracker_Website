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

        [ForeignKey("CandidateId")]
        public ApplicationUser? Candidate { get; set; }


        // ============================================================
        // VACANCY
        // ============================================================

        public int JobVacancyId { get; set; }

        [ForeignKey("JobVacancyId")]
        public JobVacancy? JobVacancy { get; set; }


        // ============================================================
        // APPLICATION STATUS
        // ============================================================

        /*
         * Examples:
         *
         * Applied
         * Under Review
         * Shortlisted
         * Not Shortlisted

         * Hiring Manager decisions are stored in separate fields and
         * must not overwrite the candidate-facing Sprint 1 status.
         */

        public string Status { get; set; } = "Applied";


        // ============================================================
        // AI CV ANALYSIS
        // ============================================================

        // Final AI matching score
        public double AIScore { get; set; } = 0;

        // AI score for required requirements
        public double AIRequiredScore { get; set; } = 0;

        // AI score for preferred requirements
        public double AIPreferredScore { get; set; } = 0;

        // AI semantic job-description score
        public double AIOverallScore { get; set; } = 0;

        // AI recommendation
        //
        // Examples:
        // High Match
        // Good Match
        // Review
        // Low Match
        // Not Analyzed

        public string AIRecommendation { get; set; }
            = "Not Analyzed";


        // Detailed AI analysis stored as JSON
        public string AIRequiredResults { get; set; } = "[]";

        public string AIPreferredResults { get; set; } = "[]";


        // Candidate position after AI ranking
        public int AIRank { get; set; } = 0;


        // ============================================================
        // HR REVIEW
        // ============================================================

        // HR has manually reviewed the candidate
        public bool HRReviewed { get; set; } = false;


        // HR selected the candidate to send to
        // the Hiring Manager
        // HR selected the candidate to be sent
        // to the Hiring Manager
        public bool HRShortlisted { get; set; } = false;

        // HR's reason/comment for forwarding the candidate


        // ============================================================
        // HIRING MANAGER REVIEW
        // ============================================================

        // Hiring Manager has reviewed the candidate
        public bool HiringManagerReviewed { get; set; } = false;


        // Hiring Manager selected the candidate
        // as a final candidate
        public bool HiringManagerShortlisted { get; set; } = false;


        // Hiring Manager's decision
        //
        // Pending
        // Shortlisted
        // Rejected

        public string HiringManagerDecision { get; set; }
            = "Pending";


        // Optional comments/reason from Hiring Manager
        public string HiringManagerComments { get; set; } = "";


        // ============================================================
        // CV
        // ============================================================

        public string? CVFilePath { get; set; }

        // COVER LETTER

        public string? CoverLetterFilePath { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";



        // ============================================================
        // APPLICATION DATE
        // ============================================================

        public DateTime AppliedDate { get; set; }
            = DateTime.Now;
    }
}
