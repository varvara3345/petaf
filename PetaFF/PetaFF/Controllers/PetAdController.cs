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
                new PetAd { Name = "Барсик", Type = "Кот", Description = "Серый пушистый кот, пропал возле ул. Немига. Без ошейника.", DateLost = DateTime.ParseExact("10.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234567", Status = PetStatus.InSearch, Address = "ул. Немига", District = "Центральный", User = new User { Username = "Анна" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=66c47e81ea3d2d352ba09036be2392b301fda0b3-5232624-images-thumbs&n=13" },
                new PetAd { Name = "Белла", Type = "Собака", Description = "Лабрадор, светлая, дружелюбная. Убежала возле Логойского тракта.", DateLost = DateTime.ParseExact("29.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375296543210", Status = PetStatus.InSearch, Address = "Логойский тракт", District = "Советский", User = new User { Username = "Иван" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=0d8c423a6fe9d65006bc1f4da5f5f504468dd3b3-10026462-images-thumbs&n=13" },
                new PetAd { Name = "Мурка", Type = "Кошка", Description = "Черная с белыми лапками, исчезла в районе пл. Победы.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375447891234", Status = PetStatus.InSearch, Address = "пл. Победы", District = "Ленинский", User = new User { Username = "Мария" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=fcc7d3b12a1de19b9f981a9ba6a958caae2ca41c-8154230-images-thumbs&n=13" },
                new PetAd { Name = "Рекс", Type = "Собака", Description = "Овчарка, откликается на кличку. Потерян на Кальварийской.", DateLost = DateTime.ParseExact("15.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375333456789", Status = PetStatus.InSearch, Address = "ул. Кальварийская", District = "Фрунзенский", User = new User { Username = "Павел" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=0a2f7cdd30db4f741d18234aa36ef91d0cdf1a95-5342977-images-thumbs&n=13" },
                new PetAd { Name = "Снежок", Type = "Кролик", Description = "Белый декоративный кролик, пропал с двора на ул. Ангарской.", DateLost = DateTime.ParseExact("05.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375297654321", Status = PetStatus.InSearch, Address = "ул. Ангарская", District = "Заводской", User = new User { Username = "Ольга" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=d0b0952dd4b0076ba6f9756c38f23ec3a781e893-4011440-images-thumbs&n=13" },
                new PetAd { Name = "Том", Type = "Кот", Description = "Рыжий, крупный, ушёл с ул. Восточной.", DateLost = DateTime.ParseExact("22.03.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291111222", Status = PetStatus.InSearch, Address = "ул. Восточная", District = "Партизанский", User = new User { Username = "Артём" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=38c8a34d7b3d020103fd87c79126db4cb6f83b20-5875598-images-thumbs&n=13" },
                new PetAd { Name = "Лаки", Type = "Собака", Description = "Йоркширский терьер, исчез возле Орловской.", DateLost = DateTime.ParseExact("03.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375295551234", Status = PetStatus.InSearch, Address = "ул. Орловская", District = "Октябрьский", User = new User { Username = "Елена" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=c32c9a995c636279dbf07407eb20fef3f041443f902def07-5233182-images-thumbs&n=13" },
                new PetAd { Name = "Дымка", Type = "Кошка", Description = "Серая, пушистая, боится людей. Потеряна в районе Бехтерева.", DateLost = DateTime.ParseExact("18.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375445671234", Status = PetStatus.InSearch, Address = "ул. Бехтерева", District = "Московский", User = new User { Username = "Дмитрий" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=a87ab36b5931451ea41f04b3b88e5e083627ee6c-5275133-images-thumbs&n=13" },
                new PetAd { Name = "Чарли", Type = "Попугай", Description = "Зелёный волнистый попугай, умеет говорить. Вылетел в районе Брилевской.", DateLost = DateTime.ParseExact("09.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375298765432", Status = PetStatus.InSearch, Address = "ул. Брилевская", District = "Ленинский", User = new User { Username = "Светлана" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=ca19459a077fcf82b92bb59d3293e3fdc1e272b9-5285455-images-thumbs&n=13" },
                new PetAd { Name = "Грей", Type = "Собака", Description = "Хаски, голубые глаза. Пропал на Городском Вале.", DateLost = DateTime.ParseExact("07.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292345678", Status = PetStatus.InSearch, Address = "Городской Вал", District = "Центральный", User = new User { Username = "Алексей" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=4df74265a15672eb86fe0efb846aafd8543e21a8-5446295-images-thumbs&n=13" },
                new PetAd { Name = "Луна", Type = "Кошка", Description = "Трехцветная, домашняя. Убежала из квартиры на Сурганова.", DateLost = DateTime.ParseExact("30.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234888", Status = PetStatus.InSearch, Address = "ул. Сурганова", District = "Советский", User = new User { Username = "Виктория" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=a141fa98175e7119cc69e01ff8bf0cd10a9fb1ab-5482778-images-thumbs&n=13" },
                new PetAd { Name = "Шарик", Type = "Собака", Description = "Местный песик, сбежал из двора в Каменной Горке.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375299998877", Status = PetStatus.InSearch, Address = "Каменная Горка", District = "Фрунзенский", User = new User { Username = "Сергей" }, PhotoPath = "https://avatars.mds.yandex.net/get-entity_search/1966007/978078405/SUx182" },
                new PetAd { Name = "Кнопа", Type = "Кошка", Description = "Маленькая чёрная кошечка, исчезла на Слепянке.", DateLost = DateTime.ParseExact("27.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375293332211", Status = PetStatus.InSearch, Address = "Слепянка", District = "Партизанский", User = new User { Username = "Наталья" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=7e383565b750212d2898a24b04bb729436eacdfa-5239608-images-thumbs&n=13" },
                new PetAd { Name = "Ричи", Type = "Собака", Description = "Французский бульдог, последний раз видели возле Маяковского.", DateLost = DateTime.ParseExact("04.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291239999", Status = PetStatus.InSearch, Address = "ул. Маяковского", District = "Заводской", User = new User { Username = "Андрей" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=7fd9f6d88218c15fe107325131f97bc4b334043b-4119571-images-thumbs&n=13" },
                new PetAd { Name = "Жужа", Type = "Кошка", Description = "Бежево-серая, потеряна возле Зеленого Луга.", DateLost = DateTime.ParseExact("08.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292223344", Status = PetStatus.InSearch, Address = "Зеленый Луг", District = "Советский", User = new User { Username = "Татьяна" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=9961f38ae5c64920372d564bdecbbc291a88db0d-7752980-images-thumbs&n=13" },
            };
            if (id < 1 || id > fakeAds.Count)
                return NotFound();
            var ad = fakeAds[id - 1];
            return View(ad);
        }

        // Печать реального объявления
        public async Task<IActionResult> Print(int id)
        {
            var petAd = await _context.PetAds.FirstOrDefaultAsync(p => p.Id == id);
            if (petAd == null)
                return NotFound();
            return View("Print", petAd);
        }

        // Печать фейкового объявления (по имени)
        public IActionResult PrintFake(string name)
        {
            // Для простоты ищем фейковое объявление по имени (в реальном проекте лучше по id или hash)
            var fakeAds = new List<PetAd>
            {
                new PetAd { Name = "Барсик", Type = "Кот", Description = "Серый пушистый кот, пропал возле ул. Немига. Без ошейника.", DateLost = DateTime.ParseExact("10.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234567", Status = PetStatus.InSearch, Address = "ул. Немига", District = "Центральный", User = new User { Username = "Анна" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=66c47e81ea3d2d352ba09036be2392b301fda0b3-5232624-images-thumbs&n=13" },
                new PetAd { Name = "Белла", Type = "Собака", Description = "Лабрадор, светлая, дружелюбная. Убежала возле Логойского тракта.", DateLost = DateTime.ParseExact("29.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375296543210", Status = PetStatus.InSearch, Address = "Логойский тракт", District = "Советский", User = new User { Username = "Иван" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=0d8c423a6fe9d65006bc1f4da5f5f504468dd3b3-10026462-images-thumbs&n=13" },
                new PetAd { Name = "Мурка", Type = "Кошка", Description = "Черная с белыми лапками, исчезла в районе пл. Победы.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375447891234", Status = PetStatus.InSearch, Address = "пл. Победы", District = "Ленинский", User = new User { Username = "Мария" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=fcc7d3b12a1de19b9f981a9ba6a958caae2ca41c-8154230-images-thumbs&n=13" },
                new PetAd { Name = "Рекс", Type = "Собака", Description = "Овчарка, откликается на кличку. Потерян на Кальварийской.", DateLost = DateTime.ParseExact("15.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375333456789", Status = PetStatus.InSearch, Address = "ул. Кальварийская", District = "Фрунзенский", User = new User { Username = "Павел" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=0a2f7cdd30db4f741d18234aa36ef91d0cdf1a95-5342977-images-thumbs&n=13" },
                new PetAd { Name = "Снежок", Type = "Кролик", Description = "Белый декоративный кролик, пропал с двора на ул. Ангарской.", DateLost = DateTime.ParseExact("05.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375297654321", Status = PetStatus.InSearch, Address = "ул. Ангарская", District = "Заводской", User = new User { Username = "Ольга" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=d0b0952dd4b0076ba6f9756c38f23ec3a781e893-4011440-images-thumbs&n=13" },
                new PetAd { Name = "Том", Type = "Кот", Description = "Рыжий, крупный, ушёл с ул. Восточной.", DateLost = DateTime.ParseExact("22.03.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291111222", Status = PetStatus.InSearch, Address = "ул. Восточная", District = "Партизанский", User = new User { Username = "Артём" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=38c8a34d7b3d020103fd87c79126db4cb6f83b20-5875598-images-thumbs&n=13" },
                new PetAd { Name = "Лаки", Type = "Собака", Description = "Йоркширский терьер, исчез возле Орловской.", DateLost = DateTime.ParseExact("03.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375295551234", Status = PetStatus.InSearch, Address = "ул. Орловская", District = "Октябрьский", User = new User { Username = "Елена" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=c32c9a995c636279dbf07407eb20fef3f041443f902def07-5233182-images-thumbs&n=13" },
                new PetAd { Name = "Дымка", Type = "Кошка", Description = "Серая, пушистая, боится людей. Потеряна в районе Бехтерева.", DateLost = DateTime.ParseExact("18.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375445671234", Status = PetStatus.InSearch, Address = "ул. Бехтерева", District = "Московский", User = new User { Username = "Дмитрий" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=a87ab36b5931451ea41f04b3b88e5e083627ee6c-5275133-images-thumbs&n=13" },
                new PetAd { Name = "Чарли", Type = "Попугай", Description = "Зелёный волнистый попугай, умеет говорить. Вылетел в районе Брилевской.", DateLost = DateTime.ParseExact("09.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375298765432", Status = PetStatus.InSearch, Address = "ул. Брилевская", District = "Ленинский", User = new User { Username = "Светлана" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=ca19459a077fcf82b92bb59d3293e3fdc1e272b9-5285455-images-thumbs&n=13" },
                new PetAd { Name = "Грей", Type = "Собака", Description = "Хаски, голубые глаза. Пропал на Городском Вале.", DateLost = DateTime.ParseExact("07.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292345678", Status = PetStatus.InSearch, Address = "Городской Вал", District = "Центральный", User = new User { Username = "Алексей" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=4df74265a15672eb86fe0efb846aafd8543e21a8-5446295-images-thumbs&n=13" },
                new PetAd { Name = "Луна", Type = "Кошка", Description = "Трехцветная, домашняя. Убежала из квартиры на Сурганова.", DateLost = DateTime.ParseExact("30.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234888", Status = PetStatus.InSearch, Address = "ул. Сурганова", District = "Советский", User = new User { Username = "Виктория" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=a141fa98175e7119cc69e01ff8bf0cd10a9fb1ab-5482778-images-thumbs&n=13" },
                new PetAd { Name = "Шарик", Type = "Собака", Description = "Местный песик, сбежал из двора в Каменной Горке.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375299998877", Status = PetStatus.InSearch, Address = "Каменная Горка", District = "Фрунзенский", User = new User { Username = "Сергей" }, PhotoPath = "https://avatars.mds.yandex.net/get-entity_search/1966007/978078405/SUx182" },
                new PetAd { Name = "Кнопа", Type = "Кошка", Description = "Маленькая чёрная кошечка, исчезла на Слепянке.", DateLost = DateTime.ParseExact("27.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375293332211", Status = PetStatus.InSearch, Address = "Слепянка", District = "Партизанский", User = new User { Username = "Наталья" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=7e383565b750212d2898a24b04bb729436eacdfa-5239608-images-thumbs&n=13" },
                new PetAd { Name = "Ричи", Type = "Собака", Description = "Французский бульдог, последний раз видели возле Маяковского.", DateLost = DateTime.ParseExact("04.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291239999", Status = PetStatus.InSearch, Address = "ул. Маяковского", District = "Заводской", User = new User { Username = "Андрей" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=7fd9f6d88218c15fe107325131f97bc4b334043b-4119571-images-thumbs&n=13" },
                new PetAd { Name = "Жужа", Type = "Кошка", Description = "Бежево-серая, потеряна возле Зеленого Луга.", DateLost = DateTime.ParseExact("08.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292223344", Status = PetStatus.InSearch, Address = "Зеленый Луг", District = "Советский", User = new User { Username = "Татьяна" }, PhotoPath = "https://avatars.mds.yandex.net/i?id=9961f38ae5c64920372d564bdecbbc291a88db0d-7752980-images-thumbs&n=13" },
            };
            var ad = fakeAds.FirstOrDefault(f => f.Name == name);
            if (ad == null)
                return NotFound();
            return View("PrintFake", ad);
        }
    }
} 