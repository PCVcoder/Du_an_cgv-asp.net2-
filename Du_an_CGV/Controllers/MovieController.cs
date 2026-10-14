using Microsoft.AspNetCore.Mvc;

namespace Du_an_CGV.Controllers
{
    public class MovieController : Controller
    {
        public IActionResult Index(string? tab = "now-showing")
        {
            ViewBag.ActiveTab = tab;
            return View();
        }

        public IActionResult Detail(int id = 1)
        {
            ViewBag.MovieId = id;
            return View();
        }
    }
}
