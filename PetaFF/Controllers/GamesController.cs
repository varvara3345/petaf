using Microsoft.AspNetCore.Mvc;
using PetaFF.Models;

namespace PetaFF.Controllers
{
    public class GamesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PetCareQuiz()
        {
            return View();
        }

        public IActionResult BreedGuessing()
        {
            return View();
        }

        public IActionResult LostPetGuide()
        {
            return View();
        }

        public IActionResult PetFacts()
        {
            return View();
        }
    }
} 