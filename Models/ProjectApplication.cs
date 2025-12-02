using System;

namespace InternFreelance.Models
{
    public class ProjectApplication
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public int StudentId { get; set; }   // 👈 int, not string

        public string CoverLetter { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime AppliedAt { get; set; }

        public string? CvPath { get; set; }
        public string? CvOriginalFileName { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }

        public string? SubmissionUrl { get; set; }

        // Navigation
        public Project Project { get; set; } = null!;
        public AppUser Student { get; set; } = null!;
    }
}
