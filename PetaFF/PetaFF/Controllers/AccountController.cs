using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PetaFF.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неверное имя пользователя или пароль");
            return View();
        }

        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email, string firstName, string lastName, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError("", "Все поля обязательны для заполнения");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Пароли не совпадают");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("", "Пользователь с таким именем уже существует");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "Пользователь с таким email уже существует");
                return View();
            }

            var user = new User
            {
                Username = username,
                Password = password,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.PetAds)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.PetAds)
                    .ThenInclude(p => p.Likes)
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.PetAd)
                        .ThenInclude(p => p.User)
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.PetAd)
                        .ThenInclude(p => p.Comments)
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.PetAd)
                        .ThenInclude(p => p.Likes)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, IFormFile avatar)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(firstName))
                user.FirstName = firstName;

            if (!string.IsNullOrEmpty(lastName))
                user.LastName = lastName;

            if (!string.IsNullOrEmpty(phoneNumber))
                user.PhoneNumber = phoneNumber;

            if (avatar != null && avatar.Length > 0)
            {
                // Проверяем расширение файла
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Avatar", "Недопустимый формат файла. Разрешены только JPG, PNG и GIF.");
                    return RedirectToAction("Profile");
                }

                // Создаем уникальное имя файла
                string fileName = $"avatar_{userId}_{Guid.NewGuid()}{fileExtension}";
                
                // Путь для сохранения файла
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                
                // Создаем папку, если она не существует
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                string filePath = Path.Combine(uploadsFolder, fileName);
                
                // Удаляем старый аватар, если он существует
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    var oldAvatarPath = Path.Combine(_environment.WebRootPath, user.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }
                
                // Сохраняем новый аватар
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(fileStream);
                }
                
                // Обновляем путь к аватару в базе данных
                user.AvatarPath = $"/uploads/avatars/{fileName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Profile");
        }
    }
} 