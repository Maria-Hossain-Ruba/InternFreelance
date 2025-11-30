using System.Collections.Generic;

namespace InternFreelance.Models
{
    public class SmeDashboardVm
    {
        public List<Project> Projects { get; set; } = new();

        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
    }
}
