using System.ComponentModel.DataAnnotations;

namespace InternFreelance.ViewModels
{
    public class SubmitWorkViewModel
    {
        public int ApplicationId { get; set; }

        public string ProjectTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "GitHub URL is required.")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        [Display(Name = "GitHub Repository URL")]
        public string GithubUrl { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please enter a valid URL.")]
        [Display(Name = "Live Demo URL (optional)")]
        public string? LiveDemoUrl { get; set; }

        [Display(Name = "Notes for the SME (optional)")]
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
