using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Du_an_CGV.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies
                .Where(m => m.IsNowShowing)
                .OrderBy(m => m.Rank.HasValue ? m.Rank.Value : 999)
                .ThenByDescending(m => m.ReleaseDate)
                .Take(8)
                .ToListAsync();

            var promos = await _context.NewsOffers
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.Promos = promos;
            return View(movies);
        }

        public async Task<IActionResult> News(string? category = null)
        {
            var query = _context.NewsOffers.Where(n => n.IsActive);
            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                query = query.Where(n => n.Category == category);
            }

            var newsList = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveCategory = category ?? "all";
            return View(newsList);
        }

        [HttpGet]
        public async Task<IActionResult> Rental()
        {
            ViewBag.Cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rental(HallRentalRequest model)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null) model.UserId = user.Id;
                }

                if (model.CinemaId.HasValue && model.CinemaId.Value > 0)
                {
                    var cinema = await _context.Cinemas.FindAsync(model.CinemaId.Value);
                    if (cinema != null) model.CinemaName = cinema.Name;
                }

                _context.HallRentalRequests.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gửi yêu cầu thuê rạp / vé nhóm thành công! Đội ngũ CGV sẽ liên hệ tư vấn trong 24 giờ.";
                return RedirectToAction("Rental");
            }

            ViewBag.Cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        public IActionResult Lamour()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactInquiry model)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null) model.UserId = user.Id;
                }

                _context.ContactInquiries.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gửi thông tin liên hệ thành công! CGV xin chân thành cảm ơn quý khách.";
                return RedirectToAction("Contact");
            }

            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
