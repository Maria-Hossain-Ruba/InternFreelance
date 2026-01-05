using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using InternFreelance.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // ---------------------- LOGIN ----------------------

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginVm());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ⚠️ Adjust Email/Password property names if your AppUser is different
            var user = await _db.UsersTable
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == RoleType.Student)
                return RedirectToAction("Student", "Dashboard");

            if (user.Role == RoleType.Admin)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Sme", "Dashboard");
        }

        // ---------------------- REGISTER ----------------------

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVm());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // check duplicate email
            var exists = await _db.UsersTable.AnyAsync(u => u.Email == model.Email);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            // ⚠️ Adjust properties here if your AppUser has different names
            var user = new AppUser
            {
                Email = model.Email,
                Password = model.Password,  // in a real app: hash it
                Role = model.Role
            };

            _db.UsersTable.Add(user);
            await _db.SaveChangesAsync();

            // log user in after registration
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            if (user.Role == RoleType.Student)
                return RedirectToAction("Student", "Dashboard");

            if (user.Role == RoleType.Admin)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Sme", "Dashboard");
        }

        // ---------------------- LOGOUT ----------------------

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // clear everything related to this user
            HttpContext.Session.Clear();

            // go back to home or landing page
            return RedirectToAction("Index", "Home");
        }
    }
}
