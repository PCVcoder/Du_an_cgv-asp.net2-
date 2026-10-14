using Microsoft.AspNetCore.Mvc;

namespace Du_an_CGV.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index(int? movieId, int? cinemaId)
        {
            ViewBag.MovieId = movieId ?? 1;
            ViewBag.CinemaId = cinemaId ?? 1;
            return View();
        }

        public IActionResult MyTickets()
        {
            return View();
        }
    }
}
