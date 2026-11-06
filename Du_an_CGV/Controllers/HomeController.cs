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
                .Where(m => !m.IsDeleted && m.IsNowShowing && (!m.EndDate.HasValue || m.EndDate.Value.Date >= DateTime.Today))
                .OrderBy(m => m.Rank.HasValue ? m.Rank.Value : 999)
                .ThenByDescending(m => m.ReleaseDate)
                .Take(8)
                .ToListAsync();

            var promos = await _context.NewsOffers
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.Promos = promos;
            return View(movies);
        }

        public async Task<IActionResult> News(string? category = null)
        {
            var query = _context.NewsOffers.Where(n => !n.IsDeleted && n.IsActive);
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
            ViewBag.Cinemas = await _context.Cinemas
                .Where(c => !c.IsDeleted)
                .Include(c => c.CinemaHalls)
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Movies = await _context.Movies
                .Where(m => !m.IsDeleted && m.IsNowShowing)
                .OrderBy(m => m.Title)
                .ToListAsync();

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
                    var cinema = await _context.Cinemas
                        .Include(c => c.CinemaHalls)
                        .FirstOrDefaultAsync(c => c.Id == model.CinemaId.Value);

                    if (cinema != null)
                    {
                        model.CinemaName = cinema.Name;
                        var maxHall = cinema.CinemaHalls.Any() ? cinema.CinemaHalls.Max(h => h.TotalSeats) : 0;
                        var totalCapacity = cinema.CinemaHalls.Sum(h => h.TotalSeats);

                        // Đánh giá sơ bộ nếu số khách vượt sức chứa phòng đơn lớn nhất
                        if (maxHall > 0 && model.GuestCount > maxHall)
                        {
                            var autoNote = $"[HỆ THỐNG ĐÁNH GIÁ: Khách đặt {model.GuestCount} người > Sức chứa phòng lớn nhất của {cinema.Name} ({maxHall} ghế, tổng cả rạp: {totalCapacity} ghế). Đề xuất: Tư vấn chia {Math.Ceiling((double)model.GuestCount / maxHall)} phòng chiếu song song hoặc chuyển sang cụm rạp quy mô lớn hơn.]";
                            model.AdminNotes = string.IsNullOrWhiteSpace(model.AdminNotes) ? autoNote : $"{autoNote}\n{model.AdminNotes}";
                        }
                    }
                }

                _context.HallRentalRequests.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gửi yêu cầu thuê rạp / vé nhóm thành công! Đội ngũ tư vấn sự kiện CGV sẽ liên hệ hỗ trợ phương án và báo giá tối ưu trong 24 giờ.";
                return RedirectToAction("Rental");
            }

            ViewBag.Cinemas = await _context.Cinemas
                .Where(c => !c.IsDeleted)
                .Include(c => c.CinemaHalls)
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Movies = await _context.Movies
                .Where(m => !m.IsDeleted && m.IsNowShowing)
                .OrderBy(m => m.Title)
                .ToListAsync();

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
