using InternFreelance.Models;

namespace InternFreelance.Models
{
    public class AppUser
    {
        // Student profile extras
        public string? Headline { get; set; }      // "Frontend student – React + ASP.NET"
        public string? Bio { get; set; }           // Short about text
        public string? SkillsCsv { get; set; }     // "React, ASP.NET, SQL"
        public string? GithubUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? University { get; set; }
      

        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public RoleType Role { get; set; }
    }
}

