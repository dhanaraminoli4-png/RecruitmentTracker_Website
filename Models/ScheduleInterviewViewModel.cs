using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models
{
    public class ScheduleInterviewViewModel
    {
        [Required]
        [Display(Name = "Job")]
        public int JobVacancyId { get; set; }


        [Required(ErrorMessage = "Select at least one candidate.")]
        public List<int> SelectedApplicationIds { get; set; } = new();


        [Required]
        [Display(Name = "Interview Round")]
        public int InterviewRoundId { get; set; }


        [Required]
        [Display(Name = "Interview Type")]
        public int InterviewTypeId { get; set; }


        [Required]
        [DataType(DataType.Date)]
        public DateTime InterviewDate { get; set; }


        [Required]
        public TimeSpan InterviewTime { get; set; }


        [Required]
        [Range(10, 480)]
        public int DurationMinutes { get; set; } = 60;


        public string? Location { get; set; }

        public string? MeetingLink { get; set; }


        public List<string> SelectedInterviewerIds { get; set; } = new();


        // Dropdown data
        public List<SelectListItem> Jobs { get; set; } = new();

        public List<SelectListItem> InterviewRounds { get; set; } = new();

        public List<SelectListItem> InterviewTypes { get; set; } = new();

        public List<SelectListItem> Interviewers { get; set; } = new();

        public int? AssessmentTemplateId { get; set; }

        public List<SelectListItem> Assessments { get; set; }
            = new List<SelectListItem>();
    }
}