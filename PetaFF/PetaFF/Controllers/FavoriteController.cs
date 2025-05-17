using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PetaFF.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoriteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.PetAd)
                    .ThenInclude(p => p.User)
                .Include(f => f.PetAd)
                    .ThenInclude(p => p.Comments)
                .Include(f => f.PetAd)
                    .ThenInclude(p => p.Likes)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int petAdId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.PetAdId == petAdId && f.UserId == userId);

            if (existingFavorite != null)
            {
                _context.Favorites.Remove(existingFavorite);
            }
            else
            {
                var favorite = new Favorite
                {
                    PetAdId = petAdId,
                    UserId = userId.Value,
                    CreatedAt = DateTime.Now
                };
                _context.Favorites.Add(favorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "PetAd", new { id = petAdId });
        }
    }
} 