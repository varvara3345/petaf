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

        public IActionResult Index()
        {
            var latestAds = _context.PetAds
                .Include(a => a.Comments)
                .Include(a => a.Likes)
                .OrderByDescending(a => a.Id)
                .Take(4)
                .ToList();

            return View(latestAds);
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
