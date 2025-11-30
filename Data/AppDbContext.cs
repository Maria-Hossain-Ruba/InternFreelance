using InternFreelance.Models;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> UsersTable { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectApplication> Applications { get; set; } = null!;
    }
}
