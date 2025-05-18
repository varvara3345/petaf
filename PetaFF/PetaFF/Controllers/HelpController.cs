using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using PetaFF.Models;

namespace PetaFF.Controllers
{
    public class HelpController : Controller
    {
        // Для простоты: храню данные в static (в реальном проекте — в БД)
        private static List<Volunteer> Volunteers = new List<Volunteer>();

        public IActionResult Index()
        {
            return View(Volunteers);
        }

        [HttpPost]
        public IActionResult AddVolunteer(Volunteer model)
        {
            if (ModelState.IsValid)
            {
                Volunteers.Add(model);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id < 0 || id >= Volunteers.Count) return RedirectToAction("Index");
            return View(Volunteers[id]);
        }

        [HttpPost]
        public IActionResult Edit(int id, Volunteer model)
        {
            if (id < 0 || id >= Volunteers.Count) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                Volunteers[id] = model;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (id >= 0 && id < Volunteers.Count)
            {
                Volunteers.RemoveAt(id);
            }
            return RedirectToAction("Index");
        }
    }
    public class Volunteer
    {
        public string Name { get; set; }
        public string Contacts { get; set; }
        public string Districts { get; set; }
        public string Comment { get; set; }
    }
} 