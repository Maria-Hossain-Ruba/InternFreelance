using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternFreelance.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Skills { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open";

        [StringLength(500)]
        public string? BriefFilePath { get; set; }          // e.g. "/project-files/abc123_brief.pdf"

        [StringLength(255)]
        public string? BriefOriginalFileName { get; set; }  // what SME uploaded (for display)

        [Required]
        public int OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public AppUser? Owner { get; set; }

        public virtual ICollection<ProjectApplication> Applications { get; set; } = new List<ProjectApplication>();
    }
}
