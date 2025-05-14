using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System.Threading.Tasks;

namespace PetaFF.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Введите имя пользователя и пароль");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь не найден");
                return View();
            }

            if (user.Password != password)
            {
                ModelState.AddModelError("", "Неверный пароль");
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            return RedirectToAction("Index", "Home");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || 
                string.IsNullOrWhiteSpace(user.Password) || 
                string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("", "Все поля обязательны для заполнения");
                return View(user);
            }

            // Проверка формата пароля
            if (!System.Text.RegularExpressions.Regex.IsMatch(user.Password, @"^[a-zA-Z0-9]+$"))
            {
                ModelState.AddModelError("Password", "Пароль может содержать только английские буквы и цифры");
                return View(user);
            }

            if (user.Password.Length < 6 || user.Password.Length > 50)
            {
                ModelState.AddModelError("Password", "Пароль должен быть от 6 до 50 символов");
                return View(user);
            }

            // Проверяем, не существует ли уже пользователь с таким именем или email
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Пользователь с таким именем уже существует");
                return View(user);
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Пользователь с таким email уже существует");
                return View(user);
            }

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Автоматически входим в систему после регистрации
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                ModelState.AddModelError("", "Произошла ошибка при регистрации. Попробуйте позже.");
                return View(user);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
} 