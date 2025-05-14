using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace PetaFF.Controllers
{
    public class PetAdController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PetAdController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: PetAd
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var petAds = await _context.PetAds
                .Where(p => p.UserId == userId)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .ToListAsync();

            return View(petAds);
        }

        // GET: PetAd/OtherAds
        public async Task<IActionResult> OtherAds()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var petAds = await _context.PetAds
                .Where(p => p.UserId != userId)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .ToListAsync();

            return View(petAds);
        }

        // GET: PetAd/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var petAd = await _context.PetAds
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (petAd == null)
            {
                return NotFound();
            }

            return View(petAd);
        }

        // GET: PetAd/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: PetAd/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,Description,Status,Street,ContactPhone")] PetAd petAd, IFormFile photo)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (string.IsNullOrWhiteSpace(petAd.Name) ||
                    string.IsNullOrWhiteSpace(petAd.Type) ||
                    string.IsNullOrWhiteSpace(petAd.Description) ||
                    string.IsNullOrWhiteSpace(petAd.Street) ||
                    string.IsNullOrWhiteSpace(petAd.ContactPhone))
                {
                    ModelState.AddModelError("", "Пожалуйста, заполните все обязательные поля");
                    return View(petAd);
                }

                if (photo != null && photo.Length > 0)
                {
                    // Проверяем расширение файла
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("PhotoPath", "Недопустимый формат файла. Разрешены только JPG, PNG, GIF и BMP.");
                        return View(petAd);
                    }

                    // Создаем уникальное имя файла
                    string fileName = $"{Guid.NewGuid()}{fileExtension}";
                    
                    // Путь для сохранения файла
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    
                    // Создаем папку, если она не существует
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    
                    // Сохраняем файл
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }
                    
                    // Сохраняем путь к файлу в базе данных
                    petAd.PhotoPath = $"/uploads/{fileName}";
                }

                petAd.UserId = userId.Value;
                petAd.Comments = new List<Comment>();
                petAd.Likes = new List<Like>();

                _context.Add(petAd);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Произошла ошибка при сохранении объявления: " + ex.Message);
            }
            
            return View(petAd);
        }

        // GET: PetAd/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var petAd = await _context.PetAds.FindAsync(id);
            if (petAd == null || petAd.UserId != userId)
            {
                return NotFound();
            }

            return View(petAd);
        }

        // POST: PetAd/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PetAd petAd)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != petAd.Id)
            {
                return NotFound();
            }

            if (petAd.UserId != userId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(petAd);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PetAdExists(petAd.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(petAd);
        }

        // GET: PetAd/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var petAd = await _context.PetAds
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (petAd == null)
            {
                return NotFound();
            }

            return View(petAd);
        }

        // POST: PetAd/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var petAd = await _context.PetAds
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (petAd != null)
            {
                _context.PetAds.Remove(petAd);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PetAdExists(int id)
        {
            return _context.PetAds.Any(e => e.Id == id);
        }
    }
} 