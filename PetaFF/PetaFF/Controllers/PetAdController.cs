using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetaFF.Data;
using PetaFF.Models;

namespace PetaFF.Controllers
{
    [Authorize]
    public class PetAdController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PetAdController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var ads = _context.PetAds.ToList();
            return View(ads);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(PetAd ad)
        {
            if (ModelState.IsValid)
            {
                _context.PetAds.Add(ad);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(ad);
        }
    }
} 