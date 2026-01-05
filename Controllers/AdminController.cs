using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            if (string.IsNullOrEmpty(idString)) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await _db.UsersTable.FindAsync(id);
        }

        private async Task<IActionResult?> AdminGuard()
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Admin) return Forbid();
            return null;
        }

        // ✅ Admin Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var totalUsers = await _db.UsersTable.CountAsync();
            var totalProjects = await _db.Projects.CountAsync();
            var totalApplications = await _db.Applications.CountAsync();

            var openProjects = await _db.Projects.CountAsync(p => p.Status == "Open");
            var assignedProjects = await _db.Projects.CountAsync(p => p.Status == "Assigned");
            var completedProjects = await _db.Projects.CountAsync(p => p.Status == "Completed");

            var latestProjects = await _db.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            var latestUsers = await _db.UsersTable
                .OrderByDescending(u => u.Id)
                .Take(8)
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalProjects = totalProjects;
            ViewBag.TotalApplications = totalApplications;

            ViewBag.OpenProjects = openProjects;
            ViewBag.AssignedProjects = assignedProjects;
            ViewBag.CompletedProjects = completedProjects;

            ViewBag.LatestProjects = latestProjects;
            ViewBag.LatestUsers = latestUsers;

            return View();
        }

        // ✅ Manage Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var users = await _db.UsersTable
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, RoleType role)
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var u = await _db.UsersTable.FindAsync(userId);
            if (u == null) return NotFound();

            u.Role = role;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "User role updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var u = await _db.UsersTable.FindAsync(userId);
            if (u == null) return NotFound();

            // Simple safety: don’t delete admins
            if (u.Role == RoleType.Admin)
            {
                TempData["Msg"] = "Cannot delete an Admin user.";
                return RedirectToAction(nameof(Users));
            }

            _db.UsersTable.Remove(u);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "User deleted.";
            return RedirectToAction(nameof(Users));
        }

        // ✅ Manage Projects
        [HttpGet]
        public async Task<IActionResult> Projects()
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var projects = await _db.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            var guard = await AdminGuard();
            if (guard != null) return guard;

            var p = await _db.Projects.FindAsync(projectId);
            if (p == null) return NotFound();

            _db.Projects.Remove(p);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Project deleted.";
            return RedirectToAction(nameof(Projects));
        }
    }
}
