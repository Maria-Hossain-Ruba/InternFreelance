using System.Collections.Generic;
using InternFreelance.Models;

namespace InternFreelance.ViewModels
{
    public class StudentDashboardVm
    {
        public AppUser? Student { get; set; }   // made nullable

        public int TotalApplications { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }

        public List<ProjectApplication> ActiveApplications { get; set; } = new();
        public List<ProjectApplication> CompletedProjects { get; set; } = new();
        public List<Project> RecommendedProjects { get; set; } = new();

        // notifications
        public List<ProjectApplication> RecentUpdates { get; set; } = new();
    }
}
