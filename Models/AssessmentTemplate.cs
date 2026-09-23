using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class AssessmentTemplate
    {
        public int Id { get; set; }


        // ============================================================
        // VACANCY
        // ============================================================

        [Required]
        public int JobVacancyId { get; set; }

        [ForeignKey(nameof(JobVacancyId))]
        public JobVacancy? JobVacancy { get; set; }


        // ============================================================
        // INTERVIEW ROUND
        // ============================================================

        [Required]
        public int InterviewRoundId { get; set; }

        [ForeignKey(nameof(InterviewRoundId))]
        public InterviewRound? InterviewRound { get; set; }


        // ============================================================
        // ASSESSMENT DETAILS
        // ============================================================

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = "";


        [StringLength(1000)]
        public string? Description { get; set; }


        [StringLength(3000)]
        public string? Instructions { get; set; }


        // ============================================================
        // STATUS
        // ============================================================

        public bool IsActive { get; set; } = true;


        // ============================================================
        // AUDIT
        // ============================================================

        public string CreatedBy { get; set; } = "";

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;


        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }


        // ============================================================
        // QUESTIONS / TASKS
        // ============================================================

        public ICollection<AssessmentQuestion> Questions { get; set; }
            = new List<AssessmentQuestion>();
    }
}