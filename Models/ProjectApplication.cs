using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternFreelance.Models
{
    public class ProjectApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public AppUser Student { get; set; } = null!;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string CoverLetter { get; set; } = string.Empty;

        // Pending / Accepted / Rejected / Assigned / Completed / UnderReview
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime AppliedAt { get; set; }

        [StringLength(500)]
        public string CvPath { get; set; } = string.Empty;

        [StringLength(255)]
        public string CvOriginalFileName { get; set; } = string.Empty;

        public DateTime? StatusUpdatedAt { get; set; }

        // 🔹 FINAL SUBMISSION FIELDS
        [StringLength(500)]
        public string? SubmissionUrl { get; set; }            // GitHub repo

        [StringLength(500)]
        public string? SubmissionLiveDemoUrl { get; set; }    // optional live demo

        [Column(TypeName = "nvarchar(max)")]
        public string? SubmissionNotes { get; set; }          // notes for SME

        public DateTime? SubmittedAt { get; set; }            // when submitted

        // 🔹 SME review
        [Range(1, 5)]
        public int? Rating { get; set; }                      // 1–5

        [Column(TypeName = "nvarchar(max)")]
        public string? Feedback { get; set; }                 // text feedback

        public DateTime? ReviewedAt { get; set; }             // when SME reviewed

        // 🔹 Certificate placeholders (we'll use later)
        [StringLength(500)]
        public string? CertificatePath { get; set; }

        [StringLength(50)]
        public string? CertificateCode { get; set; }

        public DateTime? CertificateIssuedAt { get; set; }
    }
}
