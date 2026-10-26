using Du_an_CGV.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CinemaController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static readonly List<string> StandardCgvCities = new()
        {
            "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Đồng Nai",
            "Hải Phòng", "Quảng Ninh", "Bà Rịa-Vũng Tàu", "Bình Định", "Bình Dương",
            "Đắk Lắk", "Trà Vinh", "Yên Bái", "Vĩnh Long", "Kiên Giang",
            "Hậu Giang", "Hà Tĩnh", "Phú Yên", "Đồng Tháp", "Bạc Liêu",
            "Hưng Yên", "Khánh Hòa", "Kon Tum", "Lạng Sơn", "Nghệ An",
            "Phú Thọ", "Quảng Ngãi", "Sóc Trăng", "Sơn La", "Tây Ninh",
            "Thái Nguyên", "Thanh Hóa", "Tiền Giang"
        };

        public async Task<IActionResult> Index(string? city = "Hồ Chí Minh")
        {
            var cinemas = await _context.Cinemas
                .Include(c => c.CinemaHalls)
                .ToListAsync();

            // Collect any distinct cities from DB that might have been typed by the user
            var dbCities = cinemas
                .Select(c => c.City?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var cities = new List<string>(StandardCgvCities);
            foreach (var dbCity in dbCities)
            {
                if (!cities.Any(c => string.Equals(c, dbCity, StringComparison.OrdinalIgnoreCase)))
                {
                    cities.Add(dbCity!);
                }
            }

            // Calculate cinema counts per city
            var cinemaCounts = cinemas
                .Where(c => !string.IsNullOrEmpty(c.City))
                .GroupBy(c => c.City!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var activeCity = string.IsNullOrWhiteSpace(city) ? "Hồ Chí Minh" : city.Trim();
            var matchedCity = cities.FirstOrDefault(c => string.Equals(c, activeCity, StringComparison.OrdinalIgnoreCase)) ?? activeCity;

            ViewBag.Cities = cities;
            ViewBag.ActiveCity = matchedCity;
            ViewBag.CinemaCounts = cinemaCounts;

            return View(cinemas);
        }

        public async Task<IActionResult> Movies(int id = 1)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.CinemaHalls)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cinema == null)
            {
                cinema = await _context.Cinemas.FirstOrDefaultAsync();
                if (cinema == null) return NotFound();
            }

            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                .Where(s => s.CinemaHall!.CinemaId == cinema.Id && s.Movie != null && s.Movie.IsNowShowing && !s.Movie.IsUpcoming)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!showtimes.Any(s => s.StartTime.Date >= DateTime.Today))
            {
                // Tự động đồng bộ lịch chiếu định dạng chuẩn cho rạp nếu rạp chưa có lịch chiếu
                await DbInitializer.EnsureAllMoviesHaveShowtimesAsync(_context);
                showtimes = await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                    .Where(s => s.CinemaHall!.CinemaId == cinema.Id && s.Movie != null && s.Movie.IsNowShowing && !s.Movie.IsUpcoming)
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();
            }

            ViewBag.Cinema = cinema;
            ViewBag.Showtimes = showtimes;
            return View(showtimes);
        }
    }
}
