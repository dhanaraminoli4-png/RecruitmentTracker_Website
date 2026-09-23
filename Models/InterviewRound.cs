using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentTracker.Models
{
    public class InterviewRound
    {
        [Key]
        public int Id { get; set; }

        // The vacancy this round belongs to
        [Required]
        public int JobVacancyId { get; set; }

        [ForeignKey(nameof(JobVacancyId))]
        public JobVacancy? JobVacancy { get; set; }

        // e.g. Technical Interview, HR Interview, Final Interview
        [Required(ErrorMessage = "Round name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // 1, 2, 3...
        [Required]
        [Range(1, 20, ErrorMessage = "Sequence must be between 1 and 20.")]
        public int SequenceNumber { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}