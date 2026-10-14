using Microsoft.AspNetCore.Mvc;

namespace Du_an_CGV.Controllers
{
    public class CinemaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Movies(int id = 1)
        {
            ViewBag.CinemaId = id;
            return View();
        }
    }
}
