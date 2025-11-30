using System.Collections.Generic;

namespace InternFreelance.Models
{
    public class StudentDashboardVm
    {
        public AppUser Student { get; set; } = null!;

        public int TotalApplications { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }

        public List<Project> RecommendedProjects { get; set; } = new();
        public List<ProjectApplication> ActiveApplications { get; set; } = new();
        public List<ProjectApplication> CompletedProjects { get; set; } = new();
    }
}
