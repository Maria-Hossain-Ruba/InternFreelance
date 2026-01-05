using System.Linq;
using System.Threading.Tasks;
using InternFreelance.Data;
using InternFreelance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternFreelance.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // Landing page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Latest open/assigned projects for the live board (right side)
            var projects = await _db.Projects
                .Where(p => p.Status == "Open" || p.Status == "Assigned")
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(projects); // Views/Home/Index.cshtml
        }
        [HttpGet("/verify/{code}")]
        public IActionResult Verify(string code)
        {
            var cert = _db.Applications
                .Include(a => a.Project)
                .Include(a => a.Student)
                .FirstOrDefault(a => a.CertificateCode == code);

            if (cert == null)
                return NotFound();

            return View(cert);
        }


        // You can keep these basic actions if you use them
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
