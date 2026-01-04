using System;
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
    public class StudentController : Controller
    {
        private readonly AppDbContext _db;

        public StudentController(AppDbContext db)
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

        // /Student/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            var studentId = user.Id;

            var completedApps = await _db.Applications
                .Include(a => a.Project)
                .Where(a =>
                    a.StudentId == studentId &&
                    (a.Status == "Completed" || a.Status == "Accepted"))
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.CompletedApps = completedApps;
            ViewBag.IsOwner = true;

            return View(user);
        }

        // /Student/ViewProfile?studentId=123
        [HttpGet]
        public async Task<IActionResult> ViewProfile(int studentId)
        {
            // Db injected via DI

            var current = await GetCurrentUser();
            if (current == null)
                return RedirectToAction("Login", "Account");

            if (current.Role != RoleType.SME && current.Role != RoleType.Admin)
                return Forbid();

            var student = await _db.UsersTable
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == RoleType.Student);

            if (student == null)
                return NotFound();

            var completedApps = await _db.Applications
                .Include(a => a.Project)
                .Where(a =>
                    a.StudentId == studentId &&
                    (a.Status == "Completed" || a.Status == "Accepted"))
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            ViewBag.CompletedApps = completedApps;
            ViewBag.IsOwner = false;

            return View("Profile", student);
        }

        // GET: /Student/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        // POST: /Student/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AppUser form)
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            user.Headline = form.Headline;
            user.Bio = form.Bio;
            user.SkillsCsv = form.SkillsCsv;
            user.GithubUrl = form.GithubUrl;
            user.PortfolioUrl = form.PortfolioUrl;
            user.LinkedInUrl = form.LinkedInUrl;
            user.University = form.University;

            await _db.SaveChangesAsync();

            return RedirectToAction("Profile");
        }

        // GET: /Student/SubmitWork/5
        [HttpGet]
        public async Task<IActionResult> SubmitWork(int id)
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            var studentId = user.Id;

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == id && a.StudentId == studentId);

            if (app == null || (app.Status != "Accepted" && app.Status != "Assigned"))
            {
                TempData["Error"] = "You are not allowed to submit work for this project.";
                return RedirectToAction("Student", "Dashboard");
            }

            var vm = new SubmitWorkViewModel
            {
                ApplicationId = app.Id,
                ProjectTitle = app.Project.Title,
                GithubUrl = app.SubmissionUrl ?? string.Empty,
                LiveDemoUrl = app.SubmissionLiveDemoUrl,
                Notes = app.SubmissionNotes
            };

            return View(vm);
        }

        // POST: /Student/SubmitWork
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitWork(SubmitWorkViewModel model)
        {
            // Db injected via DI

            var user = await GetCurrentUser();
            if (user == null || user.Role != RoleType.Student)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studentId = user.Id;

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == model.ApplicationId && a.StudentId == studentId);

            if (app == null || (app.Status != "Accepted" && app.Status != "Assigned"))
            {
                TempData["Error"] = "You are not allowed to submit work for this project.";
                return RedirectToAction("Student", "Dashboard");
            }

            app.SubmissionUrl = model.GithubUrl;
            app.SubmissionLiveDemoUrl = model.LiveDemoUrl;
            app.SubmissionNotes = model.Notes;
            app.SubmittedAt = DateTime.UtcNow;
            app.StatusUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Your work has been submitted successfully!";
            return RedirectToAction("Student", "Dashboard");
        }
    }
}
