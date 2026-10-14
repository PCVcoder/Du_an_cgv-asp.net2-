using Microsoft.AspNetCore.Mvc;

namespace Du_an_CGV.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
