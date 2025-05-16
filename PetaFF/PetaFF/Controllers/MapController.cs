using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetaFF.Data;
using PetaFF.Models;
using System.Threading.Tasks;

namespace PetaFF.Controllers
{
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly YandexMapsSettings _mapsSettings;

        public MapController(ApplicationDbContext context, IOptions<YandexMapsSettings> mapsSettings)
        {
            _context = context;
            _mapsSettings = mapsSettings.Value;
        }

        public IActionResult Index()
        {
            ViewBag.ApiKey = _mapsSettings.ApiKey;
            var petAds = _context.PetAds
                .Where(p => p.Latitude != null && p.Longitude != null)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Type,
                    p.PhotoPath,
                    Location = $"{p.Latitude},{p.Longitude}"
                })
                .ToList();

            return View(petAds);
        }

        public IActionResult Discovery()
        {
            var districts = new[]
            {
                "Центральный",
                "Фрунзенский",
                "Советский",
                "Ленинский",
                "Партизанский",
                "Заводской",
                "Московский",
                "Октябрьский"
            };

            var districtStats = districts.Select(district => new
            {
                Name = district,
                Count = _context.PetAds.Count(p => p.District == district)
            }).ToList();

            return View(districtStats);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistrictAds(string district)
        {
            if (string.IsNullOrEmpty(district))
            {
                return Json(new { error = "Район не указан" });
            }

            var ads = await _context.PetAds
                .Where(p => p.District == district)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Type,
                    p.Description,
                    p.PhotoPath,
                    p.Status,
                    p.LastSeenAddress,
                    p.ContactPhone,
                    p.DateLost
                })
                .ToListAsync();

            return Json(ads);
        }

        [HttpGet]
        public async Task<IActionResult> GetMarkers()
        {
            var petAds = await _context.PetAds
                .Include(p => p.User)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Type,
                    p.Status,
                    Address = p.Address,
                    p.PhotoPath,
                    p.Description,
                    p.ContactPhone,
                    p.DateLost,
                    p.Latitude,
                    p.Longitude
                })
                .ToListAsync();

            return Json(petAds);
        }
    }
} 