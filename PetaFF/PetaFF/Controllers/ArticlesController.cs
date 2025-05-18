using Microsoft.AspNetCore.Mvc;

namespace PetaFF.Controllers
{
    public class ArticlesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 