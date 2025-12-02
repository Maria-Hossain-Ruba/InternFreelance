using InternFreelance.Models;

namespace InternFreelance.ViewModels
{
    public class ApplyProjectVm
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string? CoverLetter { get; set; }
    }
}
