using System;
using System.Collections.Generic;

namespace InternFreelance.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public decimal? Budget { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Open";
        public string? BriefFilePath { get; set; }          // e.g. "/project-files/abc123_brief.pdf"
        public string? BriefOriginalFileName { get; set; }  // what SME uploaded (for display)


        public int OwnerId { get; set; }
        public AppUser? Owner { get; set; }

        public List<ProjectApplication> Applications { get; set; } = new();
    }
}
