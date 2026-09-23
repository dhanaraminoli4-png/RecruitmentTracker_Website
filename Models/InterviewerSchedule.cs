using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class InterviewerSchedule
    {
        public int Id { get; set; }

        [Required]
        public string InterviewerId { get; set; } = "";

        [ForeignKey(nameof(InterviewerId))]
        public ApplicationUser? Interviewer { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        public bool IsAllDay { get; set; } = false;

        [Required]
        public string ScheduleType { get; set; } = "Unavailable";

        // Available
        // Unavailable
        // Leave
        // Meeting
        // Training
        // Interview
        // Other

        public string? Title { get; set; }

        public string? Notes { get; set; }

        // true = created automatically when HR schedules interview
        public bool IsSystemGenerated { get; set; } = false;

        public int? InterviewId { get; set; }

        public string CreatedBy { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}