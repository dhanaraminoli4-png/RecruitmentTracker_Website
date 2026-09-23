using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models
{
    public class JobVacancy
    {
        [Key]
        public int Id { get; set; }

        public string JobCode { get; set; } = string.Empty;

        [Required]
        public string JobTitle { get; set; } = string.Empty;

        public string? Department { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string EmploymentType { get; set; } = string.Empty;

        public decimal? Salary { get; set; }

        [Required]
        public DateTime ClosingDate { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Responsibilities { get; set; }

        [Required]
        public string Requirements { get; set; } = string.Empty;

        public string? PreferredRequirements { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool RequiresCoverLetter { get; set; } = false;

        public string? MinimumInterviewerSeniority { get; set; }

        public int MinimumInterviewerExperience { get; set; } = 0;

        public string? InterviewerRequiredSkills { get; set; }
    }
}