using Microsoft.AspNetCore.Mvc;

namespace Du_an_CGV.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return RedirectToAction("Register");
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}
