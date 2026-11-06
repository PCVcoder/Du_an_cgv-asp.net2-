using Du_an_CGV.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovieController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? tab = "now-showing")
        {
            var isUpcoming = (tab == "upcoming");
            var today = DateTime.Today;
            var movies = await _context.Movies
                .Where(m => !m.IsDeleted && (isUpcoming 
                    ? (m.IsUpcoming && !m.IsNowShowing) 
                    : (m.IsNowShowing && !m.IsUpcoming && (!m.EndDate.HasValue || m.EndDate.Value.Date >= today))))
                .OrderBy(m => m.Rank.HasValue ? m.Rank.Value : 999)
                .ThenByDescending(m => m.ReleaseDate)
                .ToListAsync();

            ViewBag.ActiveTab = tab ?? "now-showing";
            return View(movies);
        }

        public async Task<IActionResult> Detail(int id = 1)
        {
            var now = DateTime.Now;
            var movie = await _context.Movies
                .Include(m => m.Showtimes.Where(st => !st.IsDeleted && st.EndTime >= now))
                    .ThenInclude(st => st.CinemaHall)
                        .ThenInclude(ch => ch!.Cinema)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (movie == null)
            {
                // Fallback to first available active movie
                movie = await _context.Movies
                    .Include(m => m.Showtimes.Where(st => !st.IsDeleted && st.EndTime >= now))
                        .ThenInclude(st => st.CinemaHall)
                            .ThenInclude(ch => ch!.Cinema)
                    .FirstOrDefaultAsync(m => !m.IsDeleted);
                if (movie == null) return NotFound();
            }

            ViewBag.MovieId = movie.Id;
            return View(movie);
        }
    }
}
