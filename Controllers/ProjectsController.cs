using System;
using System.Linq;
using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace InternFreelance.Controllers
{
    public class ProjectsController : Controller
    {
        // Create DbContext manually (no DI)
        private AppDbContext CreateDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=internfreelance.db");

            var ctx = new AppDbContext(optionsBuilder.Options);
            // make sure all tables (UsersTable, Projects, Applications, etc.) exist
            ctx.Database.EnsureCreated();
            return ctx;
        }

        // helper to get current user from session
        private async Task<AppUser?> GetCurrentUser(AppDbContext context)
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(idString)) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await context.UsersTable.FindAsync(id);
        }

        // GET: /Projects
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            using var _context = CreateDb();

            var projects = await _context.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // GET: /Projects/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            using var _context = CreateDb();

            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();
            return View(project);
        }

        // SME: list own projects
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            var projects = await _context.Projects
                .Where(p => p.OwnerId == user.Id)
                .Include(p => p.Applications)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // SME: create project (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            return View(new Project());
        }

        // SME: create project (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            if (!ModelState.IsValid)
                return View(project);

            project.OwnerId = user.Id;
            project.CreatedAt = DateTime.UtcNow;
            project.Status = "Open";

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // 🔥 redirect SME to dashboard after posting
            return RedirectToAction("Sme", "Dashboard");
        }

        // Student: apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int projectId, string coverLetter)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.Status == "Completed") return NotFound();

            bool already = await _context.Applications
                .AnyAsync(a => a.ProjectId == projectId && a.StudentId == user.Id);

            if (already)
            {
                TempData["Msg"] = "You already applied for this project.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var app = new ProjectApplication
            {
                ProjectId = projectId,
                StudentId = user.Id,
                CoverLetter = coverLetter,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Application submitted.";
            return RedirectToAction("Details", new { id = projectId });
        }

        // Student: my applications
        // Student: apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int projectId, string coverLetter, IFormFile cvFile)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.Status == "Completed") return NotFound();

            bool already = await _context.Applications
                .AnyAsync(a => a.ProjectId == projectId && a.StudentId == user.Id);

            if (already)
            {
                TempData["Msg"] = "You already applied for this project.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // ✅ REQUIRE CV
            if (cvFile == null || cvFile.Length == 0)
            {
                TempData["Msg"] = "Please upload your CV (PDF or DOCX).";
                return RedirectToAction("Details", new { id = projectId });
            }

            // ✅ Save CV under wwwroot/cv
            var rootPath = Directory.GetCurrentDirectory();
            var cvFolder = Path.Combine(rootPath, "wwwroot", "cv");

            if (!Directory.Exists(cvFolder))
                Directory.CreateDirectory(cvFolder);

            var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(cvFile.FileName)}";
            var filePath = Path.Combine(cvFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            // relative path for link
            var relativePath = $"/cv/{uniqueName}";

            var app = new ProjectApplication
            {
                ProjectId = projectId,
                StudentId = user.Id,
                CoverLetter = coverLetter ?? string.Empty,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow,
                CvPath = relativePath
            };

            _context.Applications.Add(app);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Application submitted with your CV.";
            return RedirectToAction("Student", "Dashboard");
        }


        // SME/Admin: view apps for a project
        [HttpGet]
        public async Task<IActionResult> Applications(int projectId)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await _context.Projects
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            return View(project);
        }

        // SME/Admin: accept application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptApplication(int appId)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await _context.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == appId);

            if (app == null || app.Project == null) return NotFound();
            if (app.Project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            var all = await _context.Applications
                .Where(a => a.ProjectId == app.ProjectId)
                .ToListAsync();

            foreach (var a in all)
                a.Status = a.Id == appId ? "Accepted" : "Rejected";

            app.Project.Status = "Assigned";
            await _context.SaveChangesAsync();

            return RedirectToAction("Applications", new { projectId = app.ProjectId });
        }

        // Student: submit deliverable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDeliverable(int appId, string submissionUrl)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var app = await _context.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == appId && a.StudentId == user.Id);

            if (app == null) return NotFound();
            if (app.Status != "Accepted") return Forbid();

            app.SubmissionUrl = submissionUrl;
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Work link submitted.";
            return RedirectToAction("MyApplications");
        }

        // SME/Admin: mark project completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int projectId)
        {
            using var _context = CreateDb();

            var user = await GetCurrentUser(_context);
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            project.Status = "Completed";
            await _context.SaveChangesAsync();

            return RedirectToAction("MyProjects");
        }
    }
}
