using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PetaFF.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var allQuery = _context.PetAds;
            var mineQuery = userId != null ? _context.PetAds.Where(p => p.UserId == userId) : _context.PetAds.Where(p => false);
            var othersQuery = userId != null ? _context.PetAds.Where(p => p.UserId != userId) : _context.PetAds;

            // Получаем все уникальные районы и типы животных (по всей базе)
            var allDistricts = await _context.PetAds.Select(p => p.District).Distinct().ToListAsync();
            var allTypes = await _context.PetAds.Select(p => p.Type).Distinct().ToListAsync();

            // --- Статистика по всем объявлениям ---
            var totalFound = await allQuery.CountAsync(p => p.Status == PetStatus.Found);
            var totalActive = await allQuery.CountAsync(p => p.Status == PetStatus.InSearch);
            var totalAll = await allQuery.CountAsync();
            var districtStats = allDistricts
                .Select(d => new DistrictStat
                {
                    District = d,
                    TotalCount = allQuery.Count(p => p.District == d),
                    ActiveCount = allQuery.Count(p => p.District == d && p.Status == PetStatus.InSearch),
                    FoundCount = allQuery.Count(p => p.District == d && p.Status == PetStatus.Found)
                })
                .OrderByDescending(s => s.TotalCount)
                .ToList();
            var typeStats = allTypes
                .Select(t => new TypeStat
                {
                    Type = t,
                    TotalCount = allQuery.Count(p => p.Type == t),
                    ActiveCount = allQuery.Count(p => p.Type == t && p.Status == PetStatus.InSearch),
                    FoundCount = allQuery.Count(p => p.Type == t && p.Status == PetStatus.Found)
                })
                .OrderByDescending(s => s.TotalCount)
                .ToList();

            // --- Статистика по моим объявлениям ---
            var myStats = new SubStatisticsViewModel();
            myStats.TotalFound = await mineQuery.CountAsync(p => p.Status == PetStatus.Found);
            myStats.TotalActive = await mineQuery.CountAsync(p => p.Status == PetStatus.InSearch);
            myStats.TotalAll = await mineQuery.CountAsync();
            myStats.DistrictStats = allDistricts.Select(d => new DistrictStat
            {
                District = d,
                TotalCount = mineQuery.Count(p => p.District == d),
                ActiveCount = mineQuery.Count(p => p.District == d && p.Status == PetStatus.InSearch),
                FoundCount = mineQuery.Count(p => p.District == d && p.Status == PetStatus.Found)
            }).OrderByDescending(s => s.TotalCount).ToList();
            myStats.TypeStats = allTypes.Select(t => new TypeStat
            {
                Type = t,
                TotalCount = mineQuery.Count(p => p.Type == t),
                ActiveCount = mineQuery.Count(p => p.Type == t && p.Status == PetStatus.InSearch),
                FoundCount = mineQuery.Count(p => p.Type == t && p.Status == PetStatus.Found)
            }).OrderByDescending(s => s.TotalCount).ToList();

            // --- Статистика по объявлениям других пользователей ---
            var othersStats = new SubStatisticsViewModel();
            othersStats.TotalFound = await othersQuery.CountAsync(p => p.Status == PetStatus.Found);
            othersStats.TotalActive = await othersQuery.CountAsync(p => p.Status == PetStatus.InSearch);
            othersStats.TotalAll = await othersQuery.CountAsync();
            othersStats.DistrictStats = allDistricts.Select(d => new DistrictStat
            {
                District = d,
                TotalCount = othersQuery.Count(p => p.District == d),
                ActiveCount = othersQuery.Count(p => p.District == d && p.Status == PetStatus.InSearch),
                FoundCount = othersQuery.Count(p => p.District == d && p.Status == PetStatus.Found)
            }).OrderByDescending(s => s.TotalCount).ToList();
            othersStats.TypeStats = allTypes.Select(t => new TypeStat
            {
                Type = t,
                TotalCount = othersQuery.Count(p => p.Type == t),
                ActiveCount = othersQuery.Count(p => p.Type == t && p.Status == PetStatus.InSearch),
                FoundCount = othersQuery.Count(p => p.Type == t && p.Status == PetStatus.Found)
            }).OrderByDescending(s => s.TotalCount).ToList();

            var viewModel = new StatisticsViewModel
            {
                TotalFound = totalFound,
                TotalActive = totalActive,
                TotalAll = totalAll,
                DistrictStats = districtStats,
                TypeStats = typeStats,
                MyStats = myStats,
                OthersStats = othersStats
            };

            return View(viewModel);
        }
    }

    public class StatisticsViewModel
    {
        public int TotalFound { get; set; }
        public int TotalActive { get; set; }
        public int TotalAll { get; set; }
        public List<DistrictStat> DistrictStats { get; set; }
        public List<TypeStat> TypeStats { get; set; }
        public SubStatisticsViewModel MyStats { get; set; }
        public SubStatisticsViewModel OthersStats { get; set; }
    }

    public class SubStatisticsViewModel
    {
        public int TotalFound { get; set; }
        public int TotalActive { get; set; }
        public int TotalAll { get; set; }
        public List<DistrictStat> DistrictStats { get; set; }
        public List<TypeStat> TypeStats { get; set; }
    }

    public class DistrictStat
    {
        public string District { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int FoundCount { get; set; }
    }

    public class TypeStat
    {
        public string Type { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int FoundCount { get; set; }
    }
} 