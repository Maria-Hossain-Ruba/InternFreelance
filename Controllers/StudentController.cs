using System.Linq;
using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Controllers
{
    public class StudentController : Controller
    {
        // -------------------------------------------------
        // DbContext helper (same style as your other controllers)
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
        // STUDENT: My profile (self view)
        // URL: /Student/Profile
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            // User id as text
            var userIdText = user.Id.ToString();

            // Compare as strings so it works whether StudentId is int or string
            var completedApps = await db.Applications
                .Include(a => a.Project)
                .Where(a =>
                    a.StudentId.ToString() == userIdText &&
                    (a.Status == "Completed" || a.Status == "Accepted"))
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.CompletedApps = completedApps;
            ViewBag.IsOwner = true; // student viewing their own profile

            return View(user); // Views/Student/Profile.cshtml
        }

        // -------------------------------------------------
        // SME / ADMIN: View a student's profile from Applications
        // URL: /Student/ViewProfile?studentId=123
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ViewProfile(string studentId)
        {
            using var db = CreateDb();

            var current = await GetCurrentUser(db);
            if (current == null)
                return RedirectToAction("Login", "Account");

            if (current.Role != RoleType.SME && current.Role != RoleType.Admin)
                return Forbid();

            if (string.IsNullOrWhiteSpace(studentId))
                return NotFound();

            // Find that student by ID (AppUser.Id is int)
            var student = await db.UsersTable
                .FirstOrDefaultAsync(u => u.Id.ToString() == studentId && u.Role == RoleType.Student);

            if (student == null)
                return NotFound();

            // Again, compare as strings to avoid int/string mismatch
            var completedApps = await db.Applications
                .Include(a => a.Project)
                .Where(a =>
                    a.StudentId.ToString() == studentId &&
                    (a.Status == "Completed" || a.Status == "Accepted"))
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.CompletedApps = completedApps;
            ViewBag.IsOwner = false; // SME/Admin viewing, so no Edit button

            // Reuse the same profile view
            return View("Profile", student);
        }

        // -------------------------------------------------
        // STUDENT: Edit profile (GET)
        // URL: /Student/EditProfile
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            return View(user); // Views/Student/EditProfile.cshtml
        }

        // -------------------------------------------------
        // STUDENT: Edit profile (POST)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AppUser form)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            // Only update profile fields (no password, no role, etc.)
            user.Headline = form.Headline;
            user.Bio = form.Bio;
            user.SkillsCsv = form.SkillsCsv;
            user.GithubUrl = form.GithubUrl;
            user.PortfolioUrl = form.PortfolioUrl;
            user.LinkedInUrl = form.LinkedInUrl;
            user.University = form.University;
          

            await db.SaveChangesAsync();

            return RedirectToAction("Profile");
        }
    }
}
