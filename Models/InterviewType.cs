using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models
{
    public class InterviewType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Interview type name is required.")]
        [StringLength(80)]
        [Display(Name = "Interview Type Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}