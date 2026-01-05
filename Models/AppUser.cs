using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternFreelance.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public RoleType Role { get; set; }

        // Student profile extras
        [StringLength(200)]
        public string? Headline { get; set; }      // "Frontend student – React + ASP.NET"

        [StringLength(1000)]
        public string? Bio { get; set; }           // Short about text

        [StringLength(500)]
        public string? SkillsCsv { get; set; }     // "React, ASP.NET, SQL"

        [StringLength(500)]
        public string? GithubUrl { get; set; }

        [StringLength(500)]
        public string? PortfolioUrl { get; set; }

        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [StringLength(200)]
        public string? University { get; set; }

        // Navigation properties
        [InverseProperty("Owner")]
        public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

        [InverseProperty("Student")]
        public virtual ICollection<ProjectApplication> Applications { get; set; } = new List<ProjectApplication>();
    }
}

