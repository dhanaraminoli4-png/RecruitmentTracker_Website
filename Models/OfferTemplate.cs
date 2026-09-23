using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentTracker.Models
{
    public class OfferTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Automatically generated from {{variables}}
        // inside the template content.
        public string Variables { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }
    }
}