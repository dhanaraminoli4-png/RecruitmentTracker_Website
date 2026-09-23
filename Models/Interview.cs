using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class Interview
    {
        public int Id { get; set; }


        // ============================================================
        // CANDIDATE APPLICATION
        // ============================================================

        [Required]
        public int JobApplicationId { get; set; }

        [ForeignKey(nameof(JobApplicationId))]
        public JobApplication? JobApplication { get; set; }


        // ============================================================
        // INTERVIEW ROUND
        // ============================================================

        [Required]
        public int InterviewRoundId { get; set; }

        [ForeignKey(nameof(InterviewRoundId))]
        public InterviewRound? InterviewRound { get; set; }


        // ============================================================
        // INTERVIEW TYPE
        // ============================================================

        [Required]
        public int InterviewTypeId { get; set; }

        [ForeignKey(nameof(InterviewTypeId))]
        public InterviewType? InterviewType { get; set; }


        // ============================================================
        // DATE / TIME
        // ============================================================

        [Required]
        public DateTime InterviewDate { get; set; }

        [Required]
        public TimeSpan InterviewTime { get; set; }

        [Required]
        public int DurationMinutes { get; set; } = 60;


        // ============================================================
        // LOCATION
        // ============================================================

        public string? Location { get; set; }

        public string? MeetingLink { get; set; }


        // ============================================================
        // ASSIGNED INTERVIEWERS
        //
        // Example:
        // interviewerId1,interviewerId2
        // ============================================================

        public string? InterviewerIds { get; set; }


        // ============================================================
        // INTERVIEW STATUS
        //
        // Scheduled
        // Completed
        // No Show
        // Cancelled
        // ============================================================

        public string Status { get; set; } = "Scheduled";


        // ============================================================
        // FEEDBACK STATUS
        //
        // Pending
        // Partially Submitted
        // Submitted
        // ============================================================

        public string FeedbackStatus { get; set; } = "Pending";


        // ============================================================
        // ROUND RESULT
        //
        // Pending
        // Passed
        // Failed
        //
        // Hiring Manager makes this decision.
        // ============================================================

        public string RoundResult { get; set; } = "Pending";


        // ============================================================
        // ROUND SCORE
        //
        // Can be used later if you want to display
        // an average/summary rating.
        // ============================================================

        public double RoundScore { get; set; } = 0;


        // ============================================================
        // HIRING MANAGER ROUND DECISION
        // ============================================================

        public string? RoundDecisionNotes { get; set; }

        public string? RoundDecisionBy { get; set; }

        public DateTime? RoundDecisionDate { get; set; }


        // ============================================================
        // COMPLETION
        // ============================================================

        public DateTime? CompletedDate { get; set; }


        // ============================================================
        // INTERVIEWER FEEDBACK
        // ============================================================

        public ICollection<InterviewFeedback> Feedbacks { get; set; }
            = new List<InterviewFeedback>();


        // ============================================================
        // ASSIGNED ASSESSMENTS
        // ============================================================

        public ICollection<InterviewAssessment> Assessments { get; set; }
            = new List<InterviewAssessment>();


        // ============================================================
        // CREATED DATE
        // ============================================================

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}