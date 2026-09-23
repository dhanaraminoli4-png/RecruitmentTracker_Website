using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }

        // Identity user
        [Required]
        public string CandidateId { get; set; } = "";

        [ForeignKey(nameof(CandidateId))]
        public ApplicationUser? Candidate { get; set; }


        // =========================================================
        // PERSONAL INFORMATION
        // =========================================================

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }


        // =========================================================
        // PROFESSIONAL INFORMATION
        // =========================================================

        [StringLength(150)]
        public string? CurrentPosition { get; set; }

        public string? Education { get; set; }

        public string? Skills { get; set; }

        public string? Experience { get; set; }


        // =========================================================
        // LINKS
        // =========================================================

        [StringLength(300)]
        public string? LinkedInUrl { get; set; }

        [StringLength(300)]
        public string? GitHubUrl { get; set; }

        [StringLength(300)]
        public string? PortfolioUrl { get; set; }


        // =========================================================
        // FILES
        // =========================================================

        public string? ProfileImagePath { get; set; }

        public string? DefaultCVPath { get; set; }

        public string? DefaultCVFileName { get; set; }


        // =========================================================
        // PROFILE STATUS
        // =========================================================

        public bool IsProfileComplete { get; set; } = false;

        public DateTime CreatedDate { get; set; } =
            DateTime.Now;

        public DateTime UpdatedDate { get; set; } =
            DateTime.Now;
    }
}