using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models
{
    public class InterviewerProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";


        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }


        [Required]
        public string Gender { get; set; } = "";


        [EmailAddress]
        public string Email { get; set; } = "";


        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";


        public IFormFile? ProfileImage { get; set; }

        public string? ExistingProfileImage { get; set; }


        // ==========================================
        // ELIGIBILITY PROFILE
        // ==========================================

        [Required]
        public string Department { get; set; } = "";


        [Required]
        [Display(Name = "Current Position")]
        public string JobTitle { get; set; } = "";


        [Required]
        [Display(Name = "Seniority Level")]
        public string SeniorityLevel { get; set; } = "";


        [Range(0, 50)]
        [Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }


        [Required]
        public string Skills { get; set; } = "";
    }
}