using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace PetaFF.Controllers
{
    [Authorize]
    public class InteractionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InteractionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int petAdId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Комментарий не может быть пустым");

            var userId = 1; // TODO: Заменить на реальный ID пользователя после добавления авторизации

            var comment = new Comment
            {
                Text = text,
                PetAdId = petAdId,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "PetAd", new { id = petAdId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int petAdId)
        {
            var userId = 1; // TODO: Заменить на реальный ID пользователя после добавления авторизации

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PetAdId == petAdId && l.UserId == userId);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
            }
            else
            {
                var like = new Like
                {
                    PetAdId = petAdId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Likes.Add(like);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "PetAd", new { id = petAdId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int petAdId, PetStatus status)
        {
            var userId = 1; // TODO: Заменить на реальный ID пользователя после добавления авторизации
            var petAd = await _context.PetAds.FindAsync(petAdId);
            
            if (petAd == null)
                return NotFound();

            if (petAd.UserId != userId)
                return Forbid();

            petAd.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "PetAd", new { id = petAdId });
        }
    }
} 