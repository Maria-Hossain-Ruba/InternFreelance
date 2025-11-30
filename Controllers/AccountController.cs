using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Controllers
{
    public class AccountController : Controller
    {
        // Create DbContext manually (no DI)
        private AppDbContext CreateDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=internfreelance.db");

            var ctx = new AppDbContext(optionsBuilder.Options);
            ctx.Database.EnsureCreated();   // make sure tables exist
            return ctx;
        }

        // =============== LOGIN ===============

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            using var _context = CreateDb();

            var user = await _context.UsersTable
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // store session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            // redirect based on role
            return user.Role switch
            {
                RoleType.Student => RedirectToAction("Student", "Dashboard"),
                RoleType.SME => RedirectToAction("Sme", "Dashboard"),
                RoleType.Admin => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // =============== REGISTER ===============

        [HttpGet]
        public IActionResult Register()
        {
            // default new user as Student
            var model = new AppUser
            {
                Role = RoleType.Student
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AppUser model)
        {
            using var _context = CreateDb();

            if (!ModelState.IsValid)
                return View(model);

            bool exists = await _context.UsersTable.AnyAsync(u => u.Email == model.Email);
            if (exists)
            {
                ViewBag.Error = "An account with this email already exists.";
                return View(model);
            }

            // save user
            _context.UsersTable.Add(model);
            await _context.SaveChangesAsync();

            // auto-login after signup
            HttpContext.Session.SetString("UserId", model.Id.ToString());
            HttpContext.Session.SetString("UserName", model.FullName);
            HttpContext.Session.SetString("UserRole", model.Role.ToString());

            // redirect based on chosen role
            return model.Role switch
            {
                RoleType.Student => RedirectToAction("Student", "Dashboard"),
                RoleType.SME => RedirectToAction("Sme", "Dashboard"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // =============== LOGOUT ===============

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
