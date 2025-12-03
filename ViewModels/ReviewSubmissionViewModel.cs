using System.ComponentModel.DataAnnotations;

namespace InternFreelance.ViewModels
{
    public class ReviewSubmissionViewModel
    {
        public int ApplicationId { get; set; }

        public string ProjectTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;

        public string? GithubUrl { get; set; }
        public string? LiveDemoUrl { get; set; }
        public string? Notes { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        [Display(Name = "Rating (1–5)")]
        public int Rating { get; set; }

        [Display(Name = "Feedback for the student")]
        [MaxLength(2000)]
        public string? Feedback { get; set; }
    }
}
