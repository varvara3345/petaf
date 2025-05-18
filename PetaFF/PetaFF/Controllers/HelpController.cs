using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetaFF.Data;
using PetaFF.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PetaFF.Controllers
{
    public class HelpController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HelpController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // Получаем все заявки для общего списка
            var allVolunteers = await _context.Volunteers
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // Получаем заявки текущего пользователя
            var userVolunteers = userId.HasValue 
                ? await _context.Volunteers
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync()
                : new List<Volunteer>();

            // Передаем оба списка в представление
            ViewBag.UserVolunteers = userVolunteers;
            return View(allVolunteers);
        }

        [HttpPost]
        public async Task<IActionResult> AddVolunteer(Volunteer model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    model.UserId = userId;
                    model.CreatedAt = DateTime.Now;

                    _context.Volunteers.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Ваша заявка успешно добавлена!";
                }
                else
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        TempData["ErrorMessage"] = error.ErrorMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Произошла ошибка при сохранении заявки: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null || volunteer.UserId != userId)
            {
                return RedirectToAction("Index");
            }

            return View(volunteer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Volunteer model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null || volunteer.UserId != userId)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                volunteer.Name = model.Name;
                volunteer.Contacts = model.Contacts;
                volunteer.Districts = model.Districts;
                volunteer.Comment = model.Comment;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Заявка успешно обновлена!";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer != null && volunteer.UserId == userId)
            {
                _context.Volunteers.Remove(volunteer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Заявка успешно удалена!";
            }

            return RedirectToAction("Index");
        }
    }
} 