using Microsoft.AspNetCore.Http;

namespace RecruitmentTracker.Models
{
    public class CandidateProfileViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? CurrentPosition { get; set; }

        public string? Education { get; set; }

        public string? Skills { get; set; }

        public string? Experience { get; set; }

        public string? LinkedInUrl { get; set; }

        public string? GitHubUrl { get; set; }

        public string? PortfolioUrl { get; set; }

        public string? ProfileImagePath { get; set; }

        public string? DefaultCVPath { get; set; }

        public string? DefaultCVFileName { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public IFormFile? CVFile { get; set; }
    }
}