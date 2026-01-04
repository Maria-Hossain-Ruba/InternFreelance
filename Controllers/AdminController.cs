using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace InternFreelance.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<AppUser?> GetCurrentUser()
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (idString == null) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await _db.UsersTable.FindAsync(id);
        }

        public async Task<IActionResult> Dashboard()
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Admin) return Forbid();

            ViewBag.UserCount = await _db.UsersTable.CountAsync();
            ViewBag.ProjectCount = await _db.Projects.CountAsync();
            ViewBag.OpenProjects = await _db.Projects.CountAsync(p => p.Status == "Open");
            ViewBag.AssignedProjects = await _db.Projects.CountAsync(p => p.Status == "Assigned");
            ViewBag.CompletedProjects = await _db.Projects.CountAsync(p => p.Status == "Completed");

            var latestProjects = await _db.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(latestProjects);
        }
    }
}
