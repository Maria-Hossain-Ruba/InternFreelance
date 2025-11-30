using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace InternFreelance.Controllers
{
    public class AdminController : Controller
    {
        // create DbContext manually (same pattern as Account/Projects)
        private AppDbContext CreateDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=internfreelance.db");

            var ctx = new AppDbContext(optionsBuilder.Options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private async Task<AppUser?> GetCurrentUser(AppDbContext context)
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (idString == null) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await context.UsersTable.FindAsync(id);
        }

        public async Task<IActionResult> Dashboard()
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Admin) return Forbid();

            ViewBag.UserCount = await _context.UsersTable.CountAsync();
            ViewBag.ProjectCount = await _context.Projects.CountAsync();
            ViewBag.OpenProjects = await _context.Projects.CountAsync(p => p.Status == "Open");
            ViewBag.AssignedProjects = await _context.Projects.CountAsync(p => p.Status == "Assigned");
            ViewBag.CompletedProjects = await _context.Projects.CountAsync(p => p.Status == "Completed");

            var latestProjects = await _context.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(latestProjects);
        }
    }
}
