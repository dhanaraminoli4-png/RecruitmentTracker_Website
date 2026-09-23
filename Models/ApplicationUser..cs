using Microsoft.AspNetCore.Identity;

namespace RecruitmentTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? ProfileImagePath { get; set; }

        public string? Department { get; set; }

        public string? JobTitle { get; set; }

        public string? SeniorityLevel { get; set; }

        public int YearsOfExperience { get; set; } = 0;

        public string? Skills { get; set; }

        public bool InterviewerProfileCompleted { get; set; } = false;

        public CandidateProfile? CandidateProfile { get; set; }

    }
}