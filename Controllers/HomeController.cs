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
        // Simple DbContext helper (same pattern as your other controllers)
        private AppDbContext CreateDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=internfreelance.db");

            var ctx = new AppDbContext(optionsBuilder.Options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        // Landing page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            using var db = CreateDb();

            // Latest open/assigned projects for the live board (right side)
            var projects = await db.Projects
                .Where(p => p.Status == "Open" || p.Status == "Assigned")
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(projects); // Views/Home/Index.cshtml
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
