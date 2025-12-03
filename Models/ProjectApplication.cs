using System;

namespace InternFreelance.Models
{
    public class ProjectApplication
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        // ✅ Proper foreign key to AppUser (Id is int)
        public int StudentId { get; set; }

        public string CoverLetter { get; set; } = string.Empty;

        // Pending / Accepted / Rejected / Assigned / Completed / UnderReview
        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; }

        public string CvPath { get; set; } = string.Empty;

        public string CvOriginalFileName { get; set; } = string.Empty;

        public DateTime? StatusUpdatedAt { get; set; }

        // 🔹 FINAL SUBMISSION FIELDS
        public string? SubmissionUrl { get; set; }            // GitHub repo
        public string? SubmissionLiveDemoUrl { get; set; }    // optional live demo
        public string? SubmissionNotes { get; set; }          // notes for SME
        public DateTime? SubmittedAt { get; set; }            // when submitted
        // 🔹 SME review
        public int? Rating { get; set; }                      // 1–5
        public string? Feedback { get; set; }                 // text feedback
        public DateTime? ReviewedAt { get; set; }             // when SME reviewed

        // 🔹 Certificate placeholders (we'll use later)
        public string? CertificatePath { get; set; }
        public string? CertificateCode { get; set; }
        public DateTime? CertificateIssuedAt { get; set; }
        // 🔹 NAVIGATION PROPERTIES
        public Project Project { get; set; } = null!;
        public AppUser Student { get; set; } = null!;
    }
}
