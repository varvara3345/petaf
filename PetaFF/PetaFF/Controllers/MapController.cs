using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetaFF.Data;
using PetaFF.Models;
using System.Threading.Tasks;
using System.Globalization;

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

            // Фейковые объявления (тот же список, что и в GetDistrictAds)
            var fakeAds = new List<dynamic>
            {
                new { District = "Центральный" }, // Барсик
                new { District = "Советский" },   // Белла
                new { District = "Ленинский" },   // Мурка
                new { District = "Фрунзенский" }, // Рекс
                new { District = "Заводской" },   // Снежок
                new { District = "Партизанский" },// Том
                new { District = "Октябрьский" }, // Лаки
                new { District = "Московский" },  // Дымка
                new { District = "Ленинский" },   // Чарли
                new { District = "Центральный" }, // Грей
                new { District = "Советский" },   // Луна
                new { District = "Фрунзенский" }, // Шарик
                new { District = "Партизанский" },// Кнопа
                new { District = "Заводской" },   // Ричи
                new { District = "Советский" },   // Жужа
            };

            var districtStats = districts.Select(district => new
            {
                Name = district,
                Count = _context.PetAds.Count(p => p.District == district)
                    + fakeAds.Count(f => f.District == district)
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

            // Реальные объявления
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
                    p.DateLost,
                    IsFake = false
                })
                .ToListAsync();

            // Фейковые объявления (тот же список, что и в OtherAds/FakeDetails)
            var fakeAds = new List<dynamic>
            {
                new { Id = 0, Name = "Барсик", Type = "Кот", Description = "Серый пушистый кот, пропал возле ул. Немига. Без ошейника.", DateLost = DateTime.ParseExact("10.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234567", Status = "В поиске", LastSeenAddress = "ул. Немига", PhotoPath = "https://images.pexels.com/photos/1276553/pexels-photo-1276553.jpeg", District = "Центральный", FakeIndex = 1, IsFake = true },
                new { Id = 0, Name = "Белла", Type = "Собака", Description = "Лабрадор, светлая, дружелюбная. Убежала возле Логойского тракта.", DateLost = DateTime.ParseExact("29.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375296543210", Status = "В поиске", LastSeenAddress = "Логойский тракт", PhotoPath = "https://images.pexels.com/photos/8700/pexels-photo.jpg", District = "Советский", FakeIndex = 2, IsFake = true },
                new { Id = 0, Name = "Мурка", Type = "Кошка", Description = "Черная с белыми лапками, исчезла в районе пл. Победы.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375447891234", Status = "В поиске", LastSeenAddress = "пл. Победы", PhotoPath = "https://images.pexels.com/photos/208984/pexels-photo-208984.jpeg", District = "Ленинский", FakeIndex = 3, IsFake = true },
                new { Id = 0, Name = "Рекс", Type = "Собака", Description = "Овчарка, откликается на кличку. Потерян на Кальварийской.", DateLost = DateTime.ParseExact("15.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375333456789", Status = "В поиске", LastSeenAddress = "ул. Кальварийская", PhotoPath = "https://images.pexels.com/photos/4587991/pexels-photo-4587991.jpeg", District = "Фрунзенский", FakeIndex = 4, IsFake = true },
                new { Id = 0, Name = "Снежок", Type = "Кролик", Description = "Белый декоративный кролик, пропал с двора на ул. Ангарской.", DateLost = DateTime.ParseExact("05.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375297654321", Status = "В поиске", LastSeenAddress = "ул. Ангарская", PhotoPath = "https://images.pexels.com/photos/326012/pexels-photo-326012.jpeg", District = "Заводской", FakeIndex = 5, IsFake = true },
                new { Id = 0, Name = "Том", Type = "Кот", Description = "Рыжий, крупный, ушёл с ул. Восточной.", DateLost = DateTime.ParseExact("22.03.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291111222", Status = "В поиске", LastSeenAddress = "ул. Восточная", PhotoPath = "https://images.pexels.com/photos/1170986/pexels-photo-1170986.jpeg", District = "Партизанский", FakeIndex = 6, IsFake = true },
                new { Id = 0, Name = "Лаки", Type = "Собака", Description = "Йоркширский терьер, исчез возле Орловской.", DateLost = DateTime.ParseExact("03.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375295551234", Status = "В поиске", LastSeenAddress = "ул. Орловская", PhotoPath = "https://images.pexels.com/photos/1108099/pexels-photo-1108099.jpeg", District = "Октябрьский", FakeIndex = 7, IsFake = true },
                new { Id = 0, Name = "Дымка", Type = "Кошка", Description = "Серая, пушистая, боится людей. Потеряна в районе Бехтерева.", DateLost = DateTime.ParseExact("18.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375445671234", Status = "В поиске", LastSeenAddress = "ул. Бехтерева", PhotoPath = "https://images.pexels.com/photos/1276553/pexels-photo-1276553.jpeg", District = "Московский", FakeIndex = 8, IsFake = true },
                new { Id = 0, Name = "Чарли", Type = "Попугай", Description = "Зелёный волнистый попугай, умеет говорить. Вылетел в районе Брилевской.", DateLost = DateTime.ParseExact("09.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375298765432", Status = "В поиске", LastSeenAddress = "ул. Брилевская", PhotoPath = "https://images.pexels.com/photos/45911/peacock-bird-plumage-color-45911.jpeg", District = "Ленинский", FakeIndex = 9, IsFake = true },
                new { Id = 0, Name = "Грей", Type = "Собака", Description = "Хаски, голубые глаза. Пропал на Городском Вале.", DateLost = DateTime.ParseExact("07.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292345678", Status = "В поиске", LastSeenAddress = "Городской Вал", PhotoPath = "https://images.pexels.com/photos/356378/pexels-photo-356378.jpeg", District = "Центральный", FakeIndex = 10, IsFake = true },
                new { Id = 0, Name = "Луна", Type = "Кошка", Description = "Трехцветная, домашняя. Убежала из квартиры на Сурганова.", DateLost = DateTime.ParseExact("30.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291234888", Status = "В поиске", LastSeenAddress = "ул. Сурганова", PhotoPath = "https://images.pexels.com/photos/45201/kitty-cat-kitten-pet-45201.jpeg", District = "Советский", FakeIndex = 11, IsFake = true },
                new { Id = 0, Name = "Шарик", Type = "Собака", Description = "Местный песик, сбежал из двора в Каменной Горке.", DateLost = DateTime.ParseExact("01.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375299998877", Status = "В поиске", LastSeenAddress = "Каменная Горка", PhotoPath = "https://images.pexels.com/photos/4587991/pexels-photo-4587991.jpeg", District = "Фрунзенский", FakeIndex = 12, IsFake = true },
                new { Id = 0, Name = "Кнопа", Type = "Кошка", Description = "Маленькая чёрная кошечка, исчезла на Слепянке.", DateLost = DateTime.ParseExact("27.04.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375293332211", Status = "В поиске", LastSeenAddress = "Слепянка", PhotoPath = "https://images.pexels.com/photos/208984/pexels-photo-208984.jpeg", District = "Партизанский", FakeIndex = 13, IsFake = true },
                new { Id = 0, Name = "Ричи", Type = "Собака", Description = "Французский бульдог, последний раз видели возле Маяковского.", DateLost = DateTime.ParseExact("04.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375291239999", Status = "В поиске", LastSeenAddress = "ул. Маяковского", PhotoPath = "https://images.pexels.com/photos/1108099/pexels-photo-1108099.jpeg", District = "Заводской", FakeIndex = 14, IsFake = true },
                new { Id = 0, Name = "Жужа", Type = "Кошка", Description = "Бежево-серая, потеряна возле Зеленого Луга.", DateLost = DateTime.ParseExact("08.05.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture), ContactPhone = "+375292223344", Status = "В поиске", LastSeenAddress = "Зеленый Луг", PhotoPath = "https://images.pexels.com/photos/45201/kitty-cat-kitten-pet-45201.jpeg", District = "Советский", FakeIndex = 15, IsFake = true },
            };

            // Добавляем только те фейковые объявления, которые соответствуют району
            var fakeForDistrict = fakeAds.Where(f => f.District == district).ToList();

            // Объединяем реальные и фейковые объявления
            var allAds = ads.Concat(fakeForDistrict);

            return Json(allAds);
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