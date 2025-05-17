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
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace PetaFF.Controllers
{
    public class PetAdController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;

        public PetAdController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _httpClient = new HttpClient();
        }

        private async Task<(double? latitude, double? longitude)> GetCoordinatesAsync(string address)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address + ", Минск");
                var response = await _httpClient.GetAsync($"https://geocode-maps.yandex.ru/1.x/?apikey=4c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c&geocode={encodedAddress}&format=json");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    
                    var pos = json["response"]["GeoObjectCollection"]["featureMember"][0]["GeoObject"]["Point"]["pos"]?.ToString();
                    if (!string.IsNullOrEmpty(pos))
                    {
                        var coords = pos.Split(' ');
                        if (coords.Length == 2)
                        {
                            return (double.Parse(coords[1]), double.Parse(coords[0]));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // В случае ошибки возвращаем null
            }
            
            return (null, null);
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
                .OrderByDescending(p => p.CreatedAt)
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
                .Include(p => p.Favorites)
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
        public async Task<IActionResult> Create([Bind("Name,Type,Description,Status,Address,District,ContactPhone,DateLost,LastSeenAddress")] PetAd petAd, IFormFile photo)
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
                    string.IsNullOrWhiteSpace(petAd.Address) ||
                    string.IsNullOrWhiteSpace(petAd.District) ||
                    string.IsNullOrWhiteSpace(petAd.ContactPhone) ||
                    petAd.DateLost == null)
                {
                    ModelState.AddModelError("", "Пожалуйста, заполните все обязательные поля");
                    return View(petAd);
                }

                // Получаем координаты по адресу
                var (latitude, longitude) = await GetCoordinatesAsync(petAd.Address);
                petAd.Latitude = latitude;
                petAd.Longitude = longitude;
                petAd.Location = latitude.HasValue && longitude.HasValue ? $"{latitude},{longitude}" : null;

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

            // Формируем список статусов из глобального enum
            var statusList = Enum.GetValues(typeof(PetaFF.Models.PetStatus))
                .Cast<PetaFF.Models.PetStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = GetEnumDisplayName(s)
                }).ToList();
            ViewBag.StatusList = statusList;

            return View(petAd);
        }

        // POST: PetAd/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,Description,Status,Address,District,ContactPhone,UserId,PhotoPath,DateLost,LastSeenAddress")] PetAd petAd, IFormFile photo)
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

            // Проверяем обязательные поля
            if (string.IsNullOrWhiteSpace(petAd.Name) ||
                string.IsNullOrWhiteSpace(petAd.Type) ||
                string.IsNullOrWhiteSpace(petAd.Description) ||
                string.IsNullOrWhiteSpace(petAd.Address) ||
                string.IsNullOrWhiteSpace(petAd.District) ||
                string.IsNullOrWhiteSpace(petAd.ContactPhone) ||
                petAd.DateLost == null)
            {
                ModelState.AddModelError("", "Пожалуйста, заполните все обязательные поля");
                var statusList = Enum.GetValues(typeof(PetaFF.Models.PetStatus))
                    .Cast<PetaFF.Models.PetStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = s.ToString(),
                        Text = GetEnumDisplayName(s)
                    }).ToList();
                ViewBag.StatusList = statusList;
                return View(petAd);
            }

            try
            {
                var existingPetAd = await _context.PetAds.FindAsync(id);
                if (existingPetAd == null)
                {
                    return NotFound();
                }

                // Если адрес изменился, получаем новые координаты
                if (existingPetAd.Address != petAd.Address)
                {
                    var (latitude, longitude) = await GetCoordinatesAsync(petAd.Address);
                    existingPetAd.Latitude = latitude;
                    existingPetAd.Longitude = longitude;
                    existingPetAd.Location = latitude.HasValue && longitude.HasValue ? $"{latitude},{longitude}" : null;
                }

                // Обновляем все поля, включая район
                existingPetAd.Name = petAd.Name;
                existingPetAd.Type = petAd.Type;
                existingPetAd.Description = petAd.Description;
                existingPetAd.Status = petAd.Status;
                existingPetAd.Address = petAd.Address;
                existingPetAd.District = petAd.District;
                existingPetAd.ContactPhone = petAd.ContactPhone;
                existingPetAd.DateLost = petAd.DateLost;
                existingPetAd.LastSeenAddress = petAd.LastSeenAddress;

                // Обрабатываем новое фото, если оно было загружено
                if (photo != null && photo.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("PhotoPath", "Недопустимый формат файла. Разрешены только JPG, PNG, GIF и BMP.");
                        petAd.PhotoPath = existingPetAd.PhotoPath;
                        var statusList = Enum.GetValues(typeof(PetaFF.Models.PetStatus))
                            .Cast<PetaFF.Models.PetStatus>()
                            .Select(s => new SelectListItem
                            {
                                Value = s.ToString(),
                                Text = GetEnumDisplayName(s)
                            }).ToList();
                        ViewBag.StatusList = statusList;
                        return View(petAd);
                    }

                    string fileName = $"{Guid.NewGuid()}{fileExtension}";
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }

                    // Удаляем старое фото, если оно существует
                    if (!string.IsNullOrEmpty(existingPetAd.PhotoPath))
                    {
                        var oldPhotoPath = Path.Combine(_environment.WebRootPath, existingPetAd.PhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                    }

                    existingPetAd.PhotoPath = $"/uploads/{fileName}";
                }

                _context.Update(existingPetAd);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Произошла ошибка при сохранении изменений: " + ex.Message);
                var statusList = Enum.GetValues(typeof(PetaFF.Models.PetStatus))
                    .Cast<PetaFF.Models.PetStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = s.ToString(),
                        Text = GetEnumDisplayName(s)
                    }).ToList();
                ViewBag.StatusList = statusList;
                return View(petAd);
            }
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
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (petAd != null)
            {
                // Удаляем файл фотографии, если он существует
                if (!string.IsNullOrEmpty(petAd.PhotoPath))
                {
                    var photoPath = Path.Combine(_environment.WebRootPath, petAd.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                }

                // Удаляем все комментарии
                _context.Comments.RemoveRange(petAd.Comments);

                // Удаляем объявление
                _context.PetAds.Remove(petAd);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PetAdExists(int id)
        {
            return _context.PetAds.Any(e => e.Id == id);
        }

        // Вспомогательный метод для получения DisplayName из enum
        private string GetEnumDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttribute<DisplayAttribute>();
            return attribute != null ? attribute.Name : value.ToString();
        }

        // ДЕТАЛИ ФЕЙКОВОГО ОБЪЯВЛЕНИЯ
        public IActionResult FakeDetails(int id)
        {
            // Список фейковых объявлений (тот же, что и в OtherAds.cshtml)
            var fakeAds = new List<PetAd>
            {
                new PetAd { Name = "Барсик", Type = "Кот", Description = "Серый пушистый кот, пропал возле ул. Немига. Без ошейника.", DateLost = DateTime.ParseExact("10.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234567", Status = PetStatus.InSearch, Address = "ул. Немига", District = "Центральный", User = new User { Username = "Анна" }, PhotoPath = "https://images.pexels.com/photos/1276553/pexels-photo-1276553.jpeg" },
                new PetAd { Name = "Белла", Type = "Собака", Description = "Лабрадор, светлая, дружелюбная. Убежала возле Логойского тракта.", DateLost = DateTime.ParseExact("29.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375296543210", Status = PetStatus.InSearch, Address = "Логойский тракт", District = "Советский", User = new User { Username = "Иван" }, PhotoPath = "https://images.pexels.com/photos/8700/pexels-photo.jpg" },
                new PetAd { Name = "Мурка", Type = "Кошка", Description = "Черная с белыми лапками, исчезла в районе пл. Победы.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375447891234", Status = PetStatus.InSearch, Address = "пл. Победы", District = "Ленинский", User = new User { Username = "Мария" }, PhotoPath = "https://images.pexels.com/photos/208984/pexels-photo-208984.jpeg" },
                new PetAd { Name = "Рекс", Type = "Собака", Description = "Овчарка, откликается на кличку. Потерян на Кальварийской.", DateLost = DateTime.ParseExact("15.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375333456789", Status = PetStatus.InSearch, Address = "ул. Кальварийская", District = "Фрунзенский", User = new User { Username = "Павел" }, PhotoPath = "https://images.pexels.com/photos/4587991/pexels-photo-4587991.jpeg" },
                new PetAd { Name = "Снежок", Type = "Кролик", Description = "Белый декоративный кролик, пропал с двора на ул. Ангарской.", DateLost = DateTime.ParseExact("05.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375297654321", Status = PetStatus.InSearch, Address = "ул. Ангарская", District = "Заводской", User = new User { Username = "Ольга" }, PhotoPath = "https://images.pexels.com/photos/326012/pexels-photo-326012.jpeg" },
                new PetAd { Name = "Том", Type = "Кот", Description = "Рыжий, крупный, ушёл с ул. Восточной.", DateLost = DateTime.ParseExact("22.03.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291111222", Status = PetStatus.InSearch, Address = "ул. Восточная", District = "Партизанский", User = new User { Username = "Артём" }, PhotoPath = "https://images.pexels.com/photos/1170986/pexels-photo-1170986.jpeg" },
                new PetAd { Name = "Лаки", Type = "Собака", Description = "Йоркширский терьер, исчез возле Орловской.", DateLost = DateTime.ParseExact("03.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375295551234", Status = PetStatus.InSearch, Address = "ул. Орловская", District = "Октябрьский", User = new User { Username = "Елена" }, PhotoPath = "https://images.pexels.com/photos/1108099/pexels-photo-1108099.jpeg" },
                new PetAd { Name = "Дымка", Type = "Кошка", Description = "Серая, пушистая, боится людей. Потеряна в районе Бехтерева.", DateLost = DateTime.ParseExact("18.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375445671234", Status = PetStatus.InSearch, Address = "ул. Бехтерева", District = "Московский", User = new User { Username = "Дмитрий" }, PhotoPath = "https://images.pexels.com/photos/1276553/pexels-photo-1276553.jpeg" },
                new PetAd { Name = "Чарли", Type = "Попугай", Description = "Зелёный волнистый попугай, умеет говорить. Вылетел в районе Брилевской.", DateLost = DateTime.ParseExact("09.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375298765432", Status = PetStatus.InSearch, Address = "ул. Брилевская", District = "Ленинский", User = new User { Username = "Светлана" }, PhotoPath = "https://images.pexels.com/photos/45911/peacock-bird-plumage-color-45911.jpeg" },
                new PetAd { Name = "Грей", Type = "Собака", Description = "Хаски, голубые глаза. Пропал на Городском Вале.", DateLost = DateTime.ParseExact("07.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292345678", Status = PetStatus.InSearch, Address = "Городской Вал", District = "Центральный", User = new User { Username = "Алексей" }, PhotoPath = "https://images.pexels.com/photos/356378/pexels-photo-356378.jpeg" },
                new PetAd { Name = "Луна", Type = "Кошка", Description = "Трехцветная, домашняя. Убежала из квартиры на Сурганова.", DateLost = DateTime.ParseExact("30.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234888", Status = PetStatus.InSearch, Address = "ул. Сурганова", District = "Советский", User = new User { Username = "Виктория" }, PhotoPath = "https://images.pexels.com/photos/45201/kitty-cat-kitten-pet-45201.jpeg" },
                new PetAd { Name = "Шарик", Type = "Собака", Description = "Местный песик, сбежал из двора в Каменной Горке.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375299998877", Status = PetStatus.InSearch, Address = "Каменная Горка", District = "Фрунзенский", User = new User { Username = "Сергей" }, PhotoPath = "https://images.pexels.com/photos/4587991/pexels-photo-4587991.jpeg" },
                new PetAd { Name = "Кнопа", Type = "Кошка", Description = "Маленькая чёрная кошечка, исчезла на Слепянке.", DateLost = DateTime.ParseExact("27.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375293332211", Status = PetStatus.InSearch, Address = "Слепянка", District = "Партизанский", User = new User { Username = "Наталья" }, PhotoPath = "https://images.pexels.com/photos/208984/pexels-photo-208984.jpeg" },
                new PetAd { Name = "Ричи", Type = "Собака", Description = "Французский бульдог, последний раз видели возле Маяковского.", DateLost = DateTime.ParseExact("04.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291239999", Status = PetStatus.InSearch, Address = "ул. Маяковского", District = "Заводской", User = new User { Username = "Андрей" }, PhotoPath = "https://images.pexels.com/photos/1108099/pexels-photo-1108099.jpeg" },
                new PetAd { Name = "Жужа", Type = "Кошка", Description = "Бежево-серая, потеряна возле Зеленого Луга.", DateLost = DateTime.ParseExact("08.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292223344", Status = PetStatus.InSearch, Address = "Зеленый Луг", District = "Советский", User = new User { Username = "Татьяна" }, PhotoPath = "https://images.pexels.com/photos/45201/kitty-cat-kitten-pet-45201.jpeg" },
            };
            if (id < 1 || id > fakeAds.Count)
                return NotFound();
            var ad = fakeAds[id - 1];
            return View(ad);
        }
    }
} 