using InternFreelance.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AppUser entity
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);

                // Configure relationship with Projects (as Owner)
                entity.HasMany(e => e.OwnedProjects)
                    .WithOne(p => p.Owner)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Configure relationship with ProjectApplications (as Student)
                entity.HasMany(e => e.Applications)
                    .WithOne(a => a.Student)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Project entity
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.OwnerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.Budget)
                    .HasColumnType("decimal(18,2)");

                // Configure relationship with ProjectApplication
                entity.HasMany(e => e.Applications)
                    .WithOne(a => a.Project)
                    .HasForeignKey(a => a.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProjectApplication entity
            modelBuilder.Entity<ProjectApplication>(entity =>
            {
                entity.ToTable("Applications");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.ProjectId, e.StudentId }).IsUnique(); // Prevent duplicate applications

                entity.Property(e => e.Rating)
                    .IsRequired(false);

                // Configure relationship with Project
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Applications)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with AppUser (Student)
                entity.HasOne(e => e.Student)
                    .WithMany(u => u.Applications)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure RoleType enum storage
            modelBuilder.Entity<AppUser>()
                .Property(e => e.Role)
                .HasConversion<int>();
        }
    }
}
