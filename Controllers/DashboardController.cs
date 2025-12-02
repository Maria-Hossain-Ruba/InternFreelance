using System.Linq;
using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using InternFreelance.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Controllers
{
    public class DashboardController : Controller
    {
        // -------------------------------------------------
        // DbContext helper (no DI)
        // -------------------------------------------------
        private AppDbContext CreateDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=internfreelance.db");

            var ctx = new AppDbContext(optionsBuilder.Options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        // -------------------------------------------------
        // Current user from Session
        // -------------------------------------------------
        private async Task<AppUser?> GetCurrentUser(AppDbContext db)
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(idString)) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await db.UsersTable.FindAsync(id);
        }

        // -------------------------------------------------
        // SME DASHBOARD
        // URL: /Dashboard/Sme
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Sme()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null || (user.Role != RoleType.SME && user.Role != RoleType.Admin))
                return RedirectToAction("Login", "Account");

            var myProjects = await db.Projects
                .Where(p => p.OwnerId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var vm = new SmeDashboardVm
            {
                Projects = myProjects,
                TotalProjects = myProjects.Count,
                ActiveProjects = myProjects.Count(p => p.Status == "Open" || p.Status == "Assigned"),
                CompletedProjects = myProjects.Count(p => p.Status == "Completed")
            };

            // View: Views/Dashboard/Sme.cshtml
            return View(vm);
        }

        // -------------------------------------------------
        // STUDENT DASHBOARD
        // URL: /Dashboard/Student
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Student()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            // StudentId is int, so compare with user.Id (int)
            var studentId = user.Id;

            var apps = await db.Applications
                .Include(a => a.Project)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            // Recent updates: accepted / rejected in last few days
            var recentUpdates = apps
                .Where(a => (a.Status == "Accepted" || a.Status == "Rejected") && a.StatusUpdatedAt != null)
                .OrderByDescending(a => a.StatusUpdatedAt)
                .Take(5)
                .ToList();

            var vm = new StudentDashboardVm
            {
                Student = user,
                TotalApplications = apps.Count,
                ActiveCount = apps.Count(a =>
                    a.Status == "Pending" || a.Status == "Accepted" || a.Status == "Assigned"),
                CompletedCount = apps.Count(a => a.Status == "Completed"),
                ActiveApplications = apps.Where(a => a.Status != "Completed").ToList(),
                CompletedProjects = apps.Where(a => a.Status == "Completed").ToList(),
                RecommendedProjects = await db.Projects
                    .Where(p => p.Status == "Open")
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .ToListAsync(),
                RecentUpdates = recentUpdates
            };

            return View(vm); // Views/Dashboard/Student.cshtml
        }
    }
}
