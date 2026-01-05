using InternFreelance.Data;
using InternFreelance.Models;
using InternFreelance.ViewModels;
using InternFreelance.Helpers; // ✅ ADD
using Microsoft.AspNetCore.Hosting; // ✅ ADD
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace InternFreelance.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env; // ✅ ADD

        public ProjectsController(AppDbContext db, IWebHostEnvironment env) // ✅ UPDATE
        {
            _db = db;
            _env = env; // ✅ ADD
        }

        // -------------------------------------------------
        // Current user from Session
        // -------------------------------------------------
        private async Task<AppUser?> GetCurrentUser()
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(idString)) return null;
            if (!int.TryParse(idString, out var id)) return null;

            return await _db.UsersTable.FindAsync(id);
        }

        // -------------------------------------------------
        // LIST PROJECTS: /Projects or /Projects?search=...
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.Skills.Contains(search) ||
                    p.Description.Contains(search));
            }

            var projects = await query.ToListAsync();

            ViewBag.Search = search;
            return View(projects);
        }

        // -------------------------------------------------
        // DETAILS: /Projects/Details/5
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _db.Projects
                .Include(p => p.Owner)
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var user = await GetCurrentUser();

            bool isLoggedIn = user != null;
            bool isStudent = user != null && user.Role == RoleType.Student;

            bool projectOpen = !string.Equals(
                project.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase
            );

            ViewBag.CanApply = isStudent && projectOpen;
            ViewBag.IsLoggedIn = isLoggedIn;
            ViewBag.IsStudent = isStudent;
            ViewBag.ProjectOpen = projectOpen;

            return View(project);
        }

        // -------------------------------------------------
        // SME: MY PROJECTS: /Projects/MyProjects
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            var projects = await _db.Projects
                .Where(p => p.OwnerId == user.Id)
                .Include(p => p.Applications)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // -------------------------------------------------
        // SME: CREATE PROJECT (GET)  /Projects/Create
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            return View(new Project());
        }

        // -------------------------------------------------
        // SME: CREATE PROJECT (POST) with optional briefFile upload
        // TODO: Add file size validation
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, IFormFile? briefFile)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            if (!ModelState.IsValid)
                return View(project);

            project.OwnerId = user.Id;
            project.CreatedAt = DateTime.UtcNow;
            project.Status = "Open";

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

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            return RedirectToAction("Sme", "Dashboard");
        }

        // -------------------------------------------------
        // STUDENT: APPLY WITH CV (POST)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int projectId, string? coverLetter, IFormFile cvFile)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var project = await _db.Projects.FindAsync(projectId);
            if (project == null || project.Status == "Completed") return NotFound();

            var studentId = user.Id;

            bool already = await _db.Applications
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
                StudentId = studentId,
                CoverLetter = coverLetter ?? string.Empty,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow,
                CvPath = relativePath,
                CvOriginalFileName = cvFile.FileName
            };

            _db.Applications.Add(app);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Application submitted with your CV.";
            return RedirectToAction("Student", "Dashboard");
        }

        // -------------------------------------------------
        // SME/Admin: VIEW APPLICATIONS FOR A PROJECT
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Applications(int projectId)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await _db.Projects
                .Include(p => p.Applications)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            return View(project);
        }

        // -------------------------------------------------
        // SME/Admin: APPROVE APPLICATION
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            app.Status = "Accepted";
            app.StatusUpdatedAt = DateTime.UtcNow;

            var others = await _db.Applications
                .Where(a => a.ProjectId == app.ProjectId && a.Id != app.Id)
                .ToListAsync();

            foreach (var other in others)
            {
                other.Status = "Rejected";
                other.StatusUpdatedAt = DateTime.UtcNow;
            }

            app.Project.Status = "Assigned";

            await _db.SaveChangesAsync();

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
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            app.Status = "Rejected";
            app.StatusUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Application rejected.";
            return RedirectToAction(nameof(Applications), new { projectId = app.ProjectId });
        }

        // -------------------------------------------------
        // SME/Admin: DOWNLOAD CV FILE
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DownloadCv(int applicationId)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();
            if (user.Role != RoleType.Admin && app.Project.OwnerId != user.Id)
                return Forbid();

            if (string.IsNullOrEmpty(app.CvPath))
                return NotFound();

            var root = Directory.GetCurrentDirectory();
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
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ReviewSubmission(int applicationId)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            var app = await _db.Applications
                .Include(a => a.Project)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null) return NotFound();

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
                Rating = app.Rating ?? 5,
                Feedback = app.Feedback
            };

            return View(vm);
        }

        // -------------------------------------------------
        // SME/Admin: REVIEW A SUBMISSION (POST)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(ReviewSubmissionViewModel model)
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.SME && user.Role != RoleType.Admin) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ✅ UPDATED INCLUDE: also load Project.Owner for certificate
            var app = await _db.Applications
                .Include(a => a.Project)
                    .ThenInclude(p => p.Owner)
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

            // ✅ CERTIFICATE GENERATION (simple, safe, only once)
            if (string.IsNullOrEmpty(app.CertificatePath))
            {
                var certificateCode = $"IF-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                var certificatesDir = Path.Combine(_env.WebRootPath, "certificates");
                if (!Directory.Exists(certificatesDir))
                    Directory.CreateDirectory(certificatesDir);

                var fileName = $"certificate_app_{app.Id}.pdf";
                var fullPath = Path.Combine(certificatesDir, fileName);

                // Safe strings
                var studentName = app.Student?.FullName ?? "Student";
                var projectTitle = app.Project?.Title ?? "Project";
                var smeName = app.Project?.Owner?.FullName ?? "SME";

                CertificateGenerator.Generate(
                    filePath: fullPath,
                    studentName: studentName,
                    projectTitle: projectTitle,
                    smeName: smeName,
                    rating: app.Rating ?? 0,
                    issuedAt: DateTime.UtcNow,
                    certificateCode: certificateCode
                );

                app.CertificatePath = "/certificates/" + fileName;
                app.CertificateCode = certificateCode;
                app.CertificateIssuedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

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
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");
            if (user.Role != RoleType.Student) return Forbid();

            var studentId = user.Id;

            var app = await _db.Applications
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == appId && a.StudentId == studentId);

            if (app == null) return NotFound();
            if (app.Status != "Accepted") return Forbid();

            app.SubmissionUrl = submissionUrl;
            await _db.SaveChangesAsync();

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
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Account");

            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return NotFound();
            if (project.OwnerId != user.Id && user.Role != RoleType.Admin) return Forbid();

            project.Status = "Completed";
            await _db.SaveChangesAsync();

            return RedirectToAction("MyProjects");
        }
    }
}
