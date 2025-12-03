using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternFreelance.ViewModels;


namespace InternFreelance.Controllers
{
    public class ProjectsController : Controller
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
        // LIST PROJECTS: /Projects or /Projects?search=...
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            using var db = CreateDb();

            var query = db.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                // very simple search on title / skills / description
                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Skills.Contains(search) ||
                    p.Description.Contains(search));
            }

            var projects = await query.ToListAsync();

            ViewBag.Search = search; // optional, if you want to show it in the Projects/Index view
            return View(projects);   // Views/Projects/Index.cshtml
        }

        // -------------------------------------------------
        // DETAILS: /Projects/Details/5
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            using var db = CreateDb();

            var project = await db.Projects
                .Include(p => p.Owner)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var user = await GetCurrentUser(db); // reads Session["UserId"]

            bool isLoggedIn = user != null;
            bool isStudent = user != null && user.Role == RoleType.Student;

            bool projectOpen = !string.Equals(
                project.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase
            );

            // Only students on open projects can apply
            ViewBag.CanApply = isStudent && projectOpen;
            ViewBag.IsLoggedIn = isLoggedIn;
            ViewBag.IsStudent = isStudent;
            ViewBag.ProjectOpen = projectOpen;

            return View(project);    // Views/Projects/Details.cshtml
        }

        // -------------------------------------------------
        // SME: MY PROJECTS: /Projects/MyProjects
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            var projects = await db.Projects
                .Where(p => p.OwnerId == user.Id)
                .Include(p => p.Applications)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);   // Views/Projects/MyProjects.cshtml
        }

        // -------------------------------------------------
        // SME: CREATE PROJECT (GET)  /Projects/Create
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            return View(new Project());  // Views/Projects/Create.cshtml
        }

        // -------------------------------------------------
        // SME: CREATE PROJECT (POST) with optional briefFile upload
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, IFormFile? briefFile)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            if (!ModelState.IsValid)
                return View(project);

            project.OwnerId = user.Id;
            project.CreatedAt = DateTime.UtcNow;
            project.Status = "Open";

            // handle uploaded brief (optional)
            if (briefFile != null && briefFile.Length > 0)
            {
                var rootPath = Directory.GetCurrentDirectory();
                var folder = Path.Combine(rootPath, "wwwroot", "project-files");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(briefFile.FileName)}";
                var filePath = Path.Combine(folder, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await briefFile.CopyToAsync(stream);
                }

                project.BriefFilePath = "/project-files/" + safeFileName;
                project.BriefOriginalFileName = briefFile.FileName;
            }

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return RedirectToAction("Sme", "Dashboard");
        }

        // -------------------------------------------------
        // STUDENT: APPLY WITH CV (POST)
        // -------------------------------------------------
        // -------------------------------------------------
        // STUDENT: APPLY WITH CV (POST)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int projectId, string? coverLetter, IFormFile cvFile)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var project = await db.Projects.FindAsync(projectId);
            if (project == null || project.Status == "Completed") return NotFound();

            // ✅ StudentId is int now
            var studentId = user.Id;

            bool already = await db.Applications
                .AnyAsync(a => a.ProjectId == projectId && a.StudentId == studentId);

            if (already)
            {
                TempData["Msg"] = "You already applied for this project.";
                return RedirectToAction("Details", new { id = projectId });
            }

            if (cvFile == null || cvFile.Length == 0)
            {
                TempData["Msg"] = "Please upload your CV (PDF or DOCX).";
                return RedirectToAction("Details", new { id = projectId });
            }

            // Save CV to wwwroot/cv
            var root = Directory.GetCurrentDirectory();
            var cvFolder = Path.Combine(root, "wwwroot", "cv");

            if (!Directory.Exists(cvFolder))
                Directory.CreateDirectory(cvFolder);

            var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(cvFile.FileName)}";
            var cvPath = Path.Combine(cvFolder, uniqueName);

            using (var stream = new FileStream(cvPath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            var relativePath = "/cv/" + uniqueName;

            var app = new ProjectApplication
            {
                ProjectId = projectId,
                StudentId = studentId,                     // ✅ int FK
                CoverLetter = coverLetter ?? string.Empty,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow,
                CvPath = relativePath,
                CvOriginalFileName = cvFile.FileName
            } ;

            db.Applications.Add(app);
            await db.SaveChangesAsync();

            TempData["Msg"] = "Application submitted with your CV.";
            return RedirectToAction("Student", "Dashboard");
        }


        // -------------------------------------------------
        // SME/Admin: VIEW APPLICATIONS FOR A PROJECT
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Applications(int projectId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await db.Projects
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            // This will look for Views/Projects/Applications.cshtml
            return View(project);
        }

        // -------------------------------------------------
        // SME/Admin: APPROVE APPLICATION
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

            // SME / Admin only, and must own the project unless admin
            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            // Accept this application
            app.Status = "Accepted";
            app.StatusUpdatedAt = DateTime.UtcNow;

            // Reject all others for the same project
            var others = await db.Applications
                .Where(a => a.ProjectId == app.ProjectId && a.Id != app.Id)
                .ToListAsync();

            foreach (var other in others)
            {
                other.Status = "Rejected";
                other.StatusUpdatedAt = DateTime.UtcNow;
            }

            // Mark project as assigned
            app.Project.Status = "Assigned";

            await db.SaveChangesAsync();

            TempData["Msg"] = "Application approved and other applications rejected.";
            return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
        }

        // -------------------------------------------------
        // SME/Admin: REJECT APPLICATION
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            app.Status = "Rejected";
            app.StatusUpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            TempData["Msg"] = "Application rejected.";
            return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
        }

        // -------------------------------------------------
        // SME/Admin: DOWNLOAD CV FILE
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DownloadCv(int applicationId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();
            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            if (string.IsNullOrEmpty(app.CvPath))
                return NotFound();

            var root = Directory.GetCurrentDirectory();
            // CvPath is like "/cv/filename.ext"
            var relative = app.CvPath.TrimStart('/');
            var fullPath = Path.Combine(root, "wwwroot", relative);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var downloadName = app.CvOriginalFileName ?? "cv.pdf";

            return File(bytes, "application/octet-stream", downloadName);
        }
        // -------------------------------------------------
        // SME/Admin: REVIEW A SUBMISSION (GET)
        // URL: /Projects/ReviewSubmission/5
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ReviewSubmission(int applicationId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            var app = await db.Applications
                .Include(a => a.Project)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

            // SME must own the project (unless admin)
            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            if (app.SubmittedAt == null)
            {
                TempData["Msg"] = "The student has not submitted work yet.";
                return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
            }

            var vm = new ReviewSubmissionViewModel
            {
                ApplicationId = app.Id,
                ProjectTitle = app.Project.Title,
                StudentName = app.Student.FullName,
                GithubUrl = app.SubmissionUrl,
                LiveDemoUrl = app.SubmissionLiveDemoUrl,
                Notes = app.SubmissionNotes,
                Rating = app.Rating ?? 5,          // default 5 if not rated yet
                Feedback = app.Feedback
            };

            return View(vm);   // Views/Projects/ReviewSubmission.cshtml
        }

        // -------------------------------------------------
        // SME/Admin: REVIEW A SUBMISSION (POST)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(ReviewSubmissionViewModel model)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var app = await db.Applications
                .Include(a => a.Project)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

            if (app == null) return NotFound();

            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            if (app.SubmittedAt == null)
            {
                TempData["Msg"] = "The student has not submitted work yet.";
                return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
            }

            // Save rating + feedback
            app.Rating = model.Rating;
            app.Feedback = model.Feedback;
            app.ReviewedAt = DateTime.UtcNow;
            app.Status = "Completed";
            app.StatusUpdatedAt = DateTime.UtcNow;

            // Mark project as completed too
            app.Project.Status = "Completed";

            await db.SaveChangesAsync();

            TempData["Msg"] = "Review saved. Project marked as completed.";
            return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
        }


        // -------------------------------------------------
        // STUDENT: SUBMIT DELIVERABLE LINK (legacy simple version)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDeliverable(int appId, string submissionUrl)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var studentId = user.Id;

            var app = await db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == appId && a.StudentId == studentId);

            if (app == null) return NotFound();
            if (app.Status != "Accepted") return Forbid();

            app.SubmissionUrl = submissionUrl;
            await db.SaveChangesAsync();

            TempData["Msg"] = "Work link submitted.";
            return RedirectToAction("Student", "Dashboard");
        }

        // -------------------------------------------------
        // SME/Admin: MARK PROJECT COMPLETED
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int projectId)
        {
            using var db = CreateDb();

            var user = await GetCurrentUser(db);
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await db.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            project.Status = "Completed";
            await db.SaveChangesAsync();

            return RedirectToAction("MyProjects");
        }
    }
}
