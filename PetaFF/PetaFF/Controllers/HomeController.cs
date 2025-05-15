using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetaFF.Data;
using PetaFF.Models;
using Microsoft.EntityFrameworkCore;

namespace PetaFF.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return View(new List<PetAd>());
            }

            var petAds = await _context.PetAds
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            return View(petAds);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
