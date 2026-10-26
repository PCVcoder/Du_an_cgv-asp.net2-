using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Du_an_CGV.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public async Task<IActionResult> Index(string? tab = "dashboard")
        {
            var movies = await _context.Movies
                .OrderByDescending(m => m.Id)
                .ToListAsync();

            var cinemas = await _context.Cinemas
                .Include(c => c.CinemaHalls)
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .OrderByDescending(s => s.StartTime)
                .Take(300)
                .ToListAsync();

            var promos = await _context.NewsOffers
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var inquiries = await _context.ContactInquiries
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var rentals = await _context.HallRentalRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var bookings = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .OrderByDescending(b => b.BookingDate)
                .Take(50)
                .ToListAsync();

            var allBookings = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .Include(b => b.BookingFoodItems)
                    .ThenInclude(bf => bf.FoodItem)
                .OrderByDescending(b => b.BookingDate)
                .Take(500)
                .ToListAsync();

            var actionLogs = await _context.AdminActionLogs
                .Include(l => l.Cinema)
                .OrderByDescending(l => l.CreatedAt)
                .Take(300)
                .ToListAsync();

            var foodItems = await _context.FoodItems
                .OrderBy(f => f.Category)
                .ThenBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .ToListAsync();

            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Đã thanh toán")
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            var provinces = cinemas
                .Select(c => c.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalNowShowingMovies = movies.Count(m => m.IsNowShowing),
                TotalUpcomingMovies = movies.Count(m => m.IsUpcoming),
                TotalEndedMovies = movies.Count(m => !m.IsNowShowing && !m.IsUpcoming),
                TotalCinemas = cinemas.Count,
                TotalShowtimes = await _context.Showtimes.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalRevenue = totalRevenue,
                ActivePromosCount = promos.Count(p => p.IsActive),
                NewInquiriesCount = inquiries.Count(i => i.Status == "Chưa đọc"),
                NewRentalsCount = rentals.Count(r => r.Status == "Mới tiếp nhận"),
                TotalFoodItems = foodItems.Count,
                TotalActionLogsCount = await _context.AdminActionLogs.CountAsync(),
                Movies = movies,
                Cinemas = cinemas,
                Showtimes = showtimes,
                Promos = promos,
                Inquiries = inquiries,
                Rentals = rentals,
                Bookings = bookings,
                AllBookings = allBookings,
                FoodItems = foodItems,
                ActionLogs = actionLogs,
                Provinces = provinces
            };

            ViewBag.ActiveTab = tab ?? "dashboard";
            return View(model);
        }

        // GET: /Admin/TransactionHistory (Direct Page Access)
        public IActionResult TransactionHistory()
        {
            return RedirectToAction("Index", new { tab = "transactions" });
        }

        // GET: /Admin/ActionHistory (Direct Page Access)
        public IActionResult ActionHistory()
        {
            return RedirectToAction("Index", new { tab = "actionlogs" });
        }

        // GET: /Admin/GetTransactionDetails?id=12
        [HttpGet]
        public async Task<IActionResult> GetTransactionDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .Include(b => b.BookingFoodItems)
                    .ThenInclude(bf => bf.FoodItem)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin đơn giao dịch." });
            }

            return Json(new
            {
                success = true,
                id = booking.Id,
                bookingCode = booking.BookingCode,
                customerName = booking.CustomerName,
                customerEmail = booking.CustomerEmail,
                customerPhone = booking.CustomerPhone,
                movieTitle = booking.Showtime?.Movie?.Title ?? "Phim CGV",
                moviePoster = booking.Showtime?.Movie?.PosterUrl,
                cinemaName = booking.Showtime?.CinemaHall?.Cinema?.Name ?? "CGV Cinemas",
                cinemaCity = booking.Showtime?.CinemaHall?.Cinema?.City ?? "N/A",
                cinemaAddress = booking.Showtime?.CinemaHall?.Cinema?.Address ?? "Hệ thống rạp chiếu CGV",
                hallName = booking.Showtime?.CinemaHall?.Name ?? "Phòng chiếu",
                showtime = booking.Showtime != null ? booking.Showtime.StartTime.ToString("dd/MM/yyyy HH:mm") : "N/A",
                format = booking.Showtime?.Format ?? "2D",
                seats = booking.SeatsList,
                totalAmount = booking.TotalAmount,
                status = booking.Status,
                paymentMethod = booking.PaymentMethod,
                bookingDate = booking.BookingDate.ToString("dd/MM/yyyy HH:mm:ss"),
                foodItems = booking.BookingFoodItems.Select(bf => new
                {
                    name = bf.FoodItem?.Name ?? "Combo Bắp Nước",
                    quantity = bf.Quantity,
                    unitPrice = bf.UnitPrice,
                    totalPrice = bf.UnitPrice * bf.Quantity
                })
            });
        }

        // Helper logging method
        private async Task LogActionAsync(string actionType, string moduleName, string description, string? entityId = null, int? cinemaId = null, string status = "Thành công")
        {
            try
            {
                string? city = null;
                if (cinemaId.HasValue && cinemaId.Value > 0)
                {
                    var cinema = await _context.Cinemas.FindAsync(cinemaId.Value);
                    city = cinema?.City;
                }

                var log = new AdminActionLog
                {
                    ActionType = actionType,
                    ModuleName = moduleName,
                    Description = description,
                    EntityId = entityId,
                    PerformedBy = User.Identity?.Name ?? "Quản trị viên CGV",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    CinemaId = cinemaId,
                    City = city,
                    CreatedAt = DateTime.Now,
                    Status = status
                };

                _context.AdminActionLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silent fail for audit logging to ensure core admin business logic isn't interrupted
            }
        }

        #region ===================== CRUD PHIM (MOVIES) =====================

        [HttpGet]
        public async Task<IActionResult> GetMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();
            return Json(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(Movie movie, string? movieStatus)
        {
            // Đảm bảo chỉ có DUY NHẤT 1 trạng thái được áp dụng
            if (!string.IsNullOrEmpty(movieStatus))
            {
                movie.IsNowShowing = (movieStatus == "now-showing");
                movie.IsUpcoming = (movieStatus == "upcoming");
            }
            else
            {
                if (movie.IsNowShowing) movie.IsUpcoming = false;
            }

            if (string.IsNullOrWhiteSpace(movie.RatingColor))
            {
                movie.RatingColor = movie.Rating switch
                {
                    "T18" => "#b71c1c",
                    "T16" => "#2e7d32",
                    "T13" => "#e65100",
                    "K" => "#1565c0",
                    _ => "#2e7d32"
                };
            }

            if (string.IsNullOrWhiteSpace(movie.PosterUrl))
            {
                movie.PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=400";
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();
            if (movie.IsNowShowing)
            {
                await DbInitializer.EnsureAllMoviesHaveShowtimesAsync(_context);
            }
            await LogActionAsync("Thêm mới", "Phim", $"Thêm phim mới \"{movie.Title}\" ({movie.Duration} phút, {movie.Genre})", movie.Id.ToString());
            TempData["SuccessMessage"] = $"Đã thêm phim \"{movie.Title}\" và tự động đồng bộ lịch chiếu cho các rạp!";
            return RedirectToAction("Index", new { tab = "movies" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(Movie movie, string? movieStatus)
        {
            var existing = await _context.Movies.FindAsync(movie.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phim cần sửa.";
                return RedirectToAction("Index", new { tab = "movies" });
            }

            existing.Title = movie.Title;
            existing.Genre = movie.Genre;
            existing.Duration = movie.Duration;
            existing.Rating = movie.Rating;
            existing.RatingColor = movie.Rating switch
            {
                "T18" => "#b71c1c",
                "T16" => "#2e7d32",
                "T13" => "#e65100",
                "K" => "#1565c0",
                _ => "#2e7d32"
            };
            existing.ReleaseDate = movie.ReleaseDate;
            existing.EndDate = movie.EndDate;
            existing.PosterUrl = movie.PosterUrl;
            existing.BannerUrl = movie.BannerUrl;
            existing.TrailerUrl = movie.TrailerUrl;
            existing.Director = movie.Director;
            existing.Cast = movie.Cast;
            existing.Language = movie.Language;
            existing.Formats = movie.Formats;
            existing.Synopsis = movie.Synopsis;

            // Đảm bảo chỉ có DUY NHẤT 1 trạng thái được áp dụng
            if (!string.IsNullOrEmpty(movieStatus))
            {
                existing.IsNowShowing = (movieStatus == "now-showing");
                existing.IsUpcoming = (movieStatus == "upcoming");
            }
            else
            {
                existing.IsNowShowing = movie.IsNowShowing;
                existing.IsUpcoming = movie.IsNowShowing ? false : movie.IsUpcoming;
            }

            existing.Rank = movie.Rank;
            existing.SpecialTag = movie.SpecialTag;

            await _context.SaveChangesAsync();
            if (existing.IsNowShowing)
            {
                await DbInitializer.EnsureAllMoviesHaveShowtimesAsync(_context);
            }
            await LogActionAsync("Cập nhật", "Phim", $"Cập nhật thông tin phim \"{movie.Title}\" (ID: {movie.Id})", movie.Id.ToString());
            TempData["SuccessMessage"] = $"Đã cập nhật thông tin phim \"{movie.Title}\" và đồng bộ lịch chiếu các rạp!";
            return RedirectToAction("Index", new { tab = "movies" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                var title = movie.Title;
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Phim", $"Xóa phim \"{title}\" (ID: {id})", id.ToString());
                TempData["SuccessMessage"] = $"Đã xóa phim \"{title}\"!";
            }
            return RedirectToAction("Index", new { tab = "movies" });
        }

        #endregion

        #region ===================== CRUD CỤM RẠP (CINEMAS) =====================

        [HttpGet]
        public async Task<IActionResult> GetCinema(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null) return NotFound();
            return Json(cinema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCinema(Cinema cinema, int initialHallCount = 3)
        {
            if (string.IsNullOrWhiteSpace(cinema.ImageUrl))
            {
                cinema.ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600";
            }

            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();

            // Create initial requested number of halls (e.g. 1 to 12 halls)
            if (initialHallCount < 1) initialHallCount = 1;
            if (initialHallCount > 12) initialHallCount = 12;

            for (int i = 1; i <= initialHallCount; i++)
            {
                string hallType = i switch
                {
                    1 => "Standard 2D",
                    2 => "3D Atmos",
                    3 => "IMAX Laser",
                    4 => "L'Amour Giường Nằm",
                    5 => "4DX",
                    6 => "Gold Class",
                    _ => "Standard 2D"
                };
                int seats = (hallType == "L'Amour Giường Nằm") ? 24 : 60;
                var hall = new CinemaHall
                {
                    CinemaId = cinema.Id,
                    Name = $"Cinema {i:D2}",
                    HallType = hallType,
                    TotalSeats = seats
                };
                _context.CinemaHalls.Add(hall);
                await _context.SaveChangesAsync();

                // Auto generate seats for this hall
                await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount: 5, seatsPerRow: 12);
            }

            await LogActionAsync("Thêm mới", "Cụm rạp", $"Thêm cụm rạp mới \"{cinema.Name}\" ({cinema.City}) với {initialHallCount} phòng chiếu", cinema.Id.ToString(), cinema.Id);
            TempData["SuccessMessage"] = $"Đã thêm rạp \"{cinema.Name}\" với {initialHallCount} phòng chiếu thành công!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCinema(Cinema cinema)
        {
            var existing = await _context.Cinemas.FindAsync(cinema.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy rạp cần sửa.";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            existing.Name = cinema.Name;
            existing.City = cinema.City;
            existing.Address = cinema.Address;
            existing.Hotline = cinema.Hotline;
            existing.ImageUrl = cinema.ImageUrl;
            existing.Description = cinema.Description;

            await _context.SaveChangesAsync();
            await LogActionAsync("Cập nhật", "Cụm rạp", $"Cập nhật cụm rạp \"{cinema.Name}\" ({cinema.City})", cinema.Id.ToString(), cinema.Id);
            TempData["SuccessMessage"] = $"Đã cập nhật rạp \"{cinema.Name}\"!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCinema(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema != null)
            {
                var name = cinema.Name;
                var city = cinema.City;
                _context.Cinemas.Remove(cinema);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Cụm rạp", $"Xóa cụm rạp \"{name}\" ({city})", id.ToString());
                TempData["SuccessMessage"] = $"Đã xóa rạp \"{name}\"!";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        #endregion

        #region ===================== QUẢN LÝ PHÒNG CHIẾU (CINEMA HALLS) =====================

        [HttpGet]
        public async Task<IActionResult> GetCinemaHalls(int cinemaId)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null) return NotFound();

            var halls = await _context.CinemaHalls
                .Where(h => h.CinemaId == cinemaId)
                .OrderBy(h => h.Name)
                .Select(h => new
                {
                    h.Id,
                    h.CinemaId,
                    h.Name,
                    h.HallType,
                    h.TotalSeats,
                    SeatsCount = h.Seats.Count,
                    ShowtimesCount = h.Showtimes.Count
                })
                .ToListAsync();

            return Json(new
            {
                cinemaId = cinema.Id,
                cinemaName = cinema.Name,
                halls
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCinemaHall(int id)
        {
            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (hall == null) return NotFound();

            return Json(new
            {
                hall.Id,
                hall.CinemaId,
                CinemaName = hall.Cinema?.Name,
                hall.Name,
                hall.HallType,
                hall.TotalSeats
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCinemaHall(int cinemaId, string name, string hallType, int totalSeats = 60)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cụm rạp!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                var count = await _context.CinemaHalls.CountAsync(h => h.CinemaId == cinemaId);
                name = $"Cinema {count + 1:D2}";
            }

            var hall = new CinemaHall
            {
                CinemaId = cinemaId,
                Name = name,
                HallType = string.IsNullOrWhiteSpace(hallType) ? "Standard 2D" : hallType,
                TotalSeats = totalSeats > 0 ? totalSeats : 60
            };

            _context.CinemaHalls.Add(hall);
            await _context.SaveChangesAsync();

            // Auto generate seats for this hall
            int rowCount = (totalSeats <= 24) ? 3 : 5;
            int seatsPerRow = (totalSeats <= 24) ? 8 : 12;
            if (totalSeats >= 96)
            {
                rowCount = 8;
                seatsPerRow = 12;
            }
            await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount, seatsPerRow);

            TempData["SuccessMessage"] = $"Đã thêm phòng chiếu \"{hall.Name}\" ({hall.HallType}) cho rạp \"{cinema.Name}\"!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCinemaHall(int id, string name, string hallType, int totalSeats)
        {
            var hall = await _context.CinemaHalls.FindAsync(id);
            if (hall == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng chiếu!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            hall.Name = name;
            hall.HallType = hallType;
            if (totalSeats > 0 && totalSeats != hall.TotalSeats)
            {
                hall.TotalSeats = totalSeats;
                int rowCount = (totalSeats <= 24) ? 3 : 5;
                int seatsPerRow = (totalSeats <= 24) ? 8 : 12;
                await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount, seatsPerRow);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật phòng chiếu \"{hall.Name}\"!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCinemaHall(int id)
        {
            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .Include(h => h.Showtimes)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hall != null)
            {
                var cinemaName = hall.Cinema?.Name ?? "CGV";
                var hallName = hall.Name;
                _context.CinemaHalls.Remove(hall);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa phòng chiếu \"{hallName}\" của rạp \"{cinemaName}\"!";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdjustHallCount(int cinemaId, int targetCount)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.CinemaHalls)
                .FirstOrDefaultAsync(c => c.Id == cinemaId);

            if (cinema == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cụm rạp!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            if (targetCount < 1) targetCount = 1;
            if (targetCount > 15) targetCount = 15;

            int currentCount = cinema.CinemaHalls.Count;
            if (targetCount > currentCount)
            {
                for (int i = currentCount + 1; i <= targetCount; i++)
                {
                    string hallType = i switch
                    {
                        1 => "Standard 2D",
                        2 => "3D Atmos",
                        3 => "IMAX Laser",
                        4 => "L'Amour Giường Nằm",
                        5 => "4DX",
                        6 => "Gold Class",
                        _ => "Standard 2D"
                    };
                    int seats = (hallType == "L'Amour Giường Nằm") ? 24 : 60;
                    var hall = new CinemaHall
                    {
                        CinemaId = cinema.Id,
                        Name = $"Cinema {i:D2}",
                        HallType = hallType,
                        TotalSeats = seats
                    };
                    _context.CinemaHalls.Add(hall);
                    await _context.SaveChangesAsync();
                    await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount: 5, seatsPerRow: 12);
                }
                TempData["SuccessMessage"] = $"Đã tăng số phòng rạp \"{cinema.Name}\" từ {currentCount} lên {targetCount} phòng!";
            }
            else if (targetCount < currentCount)
            {
                var hallsToRemove = cinema.CinemaHalls
                    .OrderByDescending(h => h.Id)
                    .Take(currentCount - targetCount)
                    .ToList();

                _context.CinemaHalls.RemoveRange(hallsToRemove);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã điều chỉnh rạp \"{cinema.Name}\" xuống {targetCount} phòng chiếu!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Số phòng chiếu của rạp \"{cinema.Name}\" đã là {targetCount}!";
            }

            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        #endregion

        #region ===================== CRUD SUẤT CHIẾU (SHOWTIMES) =====================

        [HttpGet]
        public async Task<IActionResult> GetShowtime(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.CinemaHall)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (showtime == null) return NotFound();
            return Json(new
            {
                showtime.Id,
                showtime.MovieId,
                showtime.CinemaHallId,
                CinemaId = showtime.CinemaHall?.CinemaId,
                StartTime = showtime.StartTime.ToString("yyyy-MM-ddTHH:mm"),
                showtime.Price,
                showtime.Format
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckShowtimeConflict(int movieId, int cinemaHallId, DateTime startTime, int excludeShowtimeId = 0)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return Json(new { hasConflict = false, message = "Vui lòng chọn phim" });
            }

            var endTime = startTime.AddMinutes(movie.Duration);

            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == cinemaHallId && (excludeShowtimeId == 0 || s.Id != excludeShowtimeId))
                .Where(s => s.StartTime < endTime && s.EndTime > startTime)
                .FirstOrDefaultAsync();

            if (conflicting != null)
            {
                var hallName = conflicting.CinemaHall?.Name ?? "Phòng chiếu";
                var cinemaName = conflicting.CinemaHall?.Cinema?.Name ?? "CGV";
                var movieTitle = conflicting.Movie?.Title ?? "phim khác";
                var startStr = conflicting.StartTime.ToString("HH:mm dd/MM");
                var endStr = conflicting.EndTime.ToString("HH:mm");

                return Json(new
                {
                    hasConflict = true,
                    duration = movie.Duration,
                    endTime = endTime.ToString("HH:mm (dd/MM/yyyy)"),
                    message = $"⚠️ CẢNH BÁO TRÙNG LỊCH: {hallName} ({cinemaName}) đã có lịch chiếu phim \"{movieTitle}\" từ {startStr} đến {endStr}!"
                });
            }

            return Json(new
            {
                hasConflict = false,
                duration = movie.Duration,
                endTime = endTime.ToString("HH:mm (dd/MM/yyyy)"),
                message = $"✅ Khung giờ hợp lệ! Kết thúc dự kiến: {endTime:HH:mm (dd/MM/yyyy)} (Thời lượng: {movie.Duration} phút)"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShowtime(int MovieId, int CinemaHallId, DateTime StartTime, decimal Price, string Format)
        {
            var movie = await _context.Movies.FindAsync(MovieId);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phim được chọn.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var endTime = StartTime.AddMinutes(movie.Duration);

            // Kiểm tra trùng lịch chiếu trong cùng phòng chiếu
            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == CinemaHallId)
                .Where(s => s.StartTime < endTime && s.EndTime > StartTime)
                .FirstOrDefaultAsync();

            if (conflicting != null)
            {
                var hallName = conflicting.CinemaHall?.Name ?? "Phòng chiếu";
                var cinemaName = conflicting.CinemaHall?.Cinema?.Name ?? "Rạp";
                var movieTitle = conflicting.Movie?.Title ?? "Phim khác";
                var startStr = conflicting.StartTime.ToString("dd/MM/yyyy HH:mm");
                var endStr = conflicting.EndTime.ToString("HH:mm");

                TempData["ErrorMessage"] = $"⚠️ KHÔNG THỂ LƯU - TRÙNG LỊCH CHIẾU: {hallName} ({cinemaName}) đã có lịch chiếu phim \"{movieTitle}\" từ {startStr} đến {endStr}. Vui lòng chọn khung giờ khác hoặc đổi phòng chiếu!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var showtime = new Showtime
            {
                MovieId = MovieId,
                CinemaHallId = CinemaHallId,
                StartTime = StartTime,
                EndTime = endTime,
                Price = Price,
                Format = string.IsNullOrWhiteSpace(Format) ? "2D" : Format
            };

            _context.Showtimes.Add(showtime);
            await _context.SaveChangesAsync();

            var hallInfo = await _context.CinemaHalls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == CinemaHallId);
            await LogActionAsync("Thêm mới", "Suất chiếu", $"Tạo suất chiếu {showtime.Format} phim \"{movie.Title}\" ({StartTime:dd/MM/yyyy HH:mm}) tại {hallInfo?.Cinema?.Name} - {hallInfo?.Name}", showtime.Id.ToString(), hallInfo?.CinemaId);

            TempData["SuccessMessage"] = "Đã thêm suất chiếu mới thành công!";
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShowtime(int Id, int MovieId, int CinemaHallId, DateTime StartTime, decimal Price, string Format)
        {
            var existing = await _context.Showtimes.FindAsync(Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy suất chiếu cần sửa.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var movie = await _context.Movies.FindAsync(MovieId);
            var duration = movie?.Duration ?? 120;
            var endTime = StartTime.AddMinutes(duration);

            // Kiểm tra trùng lịch chiếu trong cùng phòng chiếu (loại trừ chính suất đang sửa)
            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == CinemaHallId && s.Id != Id)
                .Where(s => s.StartTime < endTime && s.EndTime > StartTime)
                .FirstOrDefaultAsync();

            if (conflicting != null)
            {
                var hallName = conflicting.CinemaHall?.Name ?? "Phòng chiếu";
                var cinemaName = conflicting.CinemaHall?.Cinema?.Name ?? "Rạp";
                var movieTitle = conflicting.Movie?.Title ?? "Phim khác";
                var startStr = conflicting.StartTime.ToString("dd/MM/yyyy HH:mm");
                var endStr = conflicting.EndTime.ToString("HH:mm");

                TempData["ErrorMessage"] = $"⚠️ KHÔNG THỂ CẬP NHẬT - TRÙNG LỊCH CHIẾU: {hallName} ({cinemaName}) đã có lịch chiếu phim \"{movieTitle}\" từ {startStr} đến {endStr}. Vui lòng chọn khung giờ khác hoặc đổi phòng chiếu!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            existing.MovieId = MovieId;
            existing.CinemaHallId = CinemaHallId;
            existing.StartTime = StartTime;
            existing.EndTime = endTime;
            existing.Price = Price;
            existing.Format = Format;

            await _context.SaveChangesAsync();

            var hallEditInfo = await _context.CinemaHalls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == CinemaHallId);
            await LogActionAsync("Cập nhật", "Suất chiếu", $"Cập nhật suất chiếu #{Id} ({StartTime:dd/MM/yyyy HH:mm}) tại {hallEditInfo?.Cinema?.Name} - {hallEditInfo?.Name}", Id.ToString(), hallEditInfo?.CinemaId);

            TempData["SuccessMessage"] = "Đã cập nhật suất chiếu thành công!";
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (showtime != null)
            {
                var hallName = showtime.CinemaHall?.Name;
                var cinemaName = showtime.CinemaHall?.Cinema?.Name;
                var cinemaId = showtime.CinemaHall?.CinemaId;

                _context.Showtimes.Remove(showtime);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Suất chiếu", $"Xóa suất chiếu #{id} ({showtime.StartTime:dd/MM/yyyy HH:mm}) tại {cinemaName} - {hallName}", id.ToString(), cinemaId);
                TempData["SuccessMessage"] = "Đã xóa suất chiếu!";
            }
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncShowtimes()
        {
            await DbInitializer.EnsureAllMoviesHaveShowtimesAsync(_context);
            var count = await _context.Showtimes.CountAsync();
            await LogActionAsync("Đồng bộ", "Suất Chiếu", $"Đồng bộ tự động lịch chiếu và khớp định dạng chuẩn cho tất cả các rạp trên toàn quốc (Tổng {count} suất chiếu)");
            TempData["SuccessMessage"] = $"Đã đồng bộ thành công lịch chiếu và định dạng chuẩn cho toàn bộ hệ thống rạp CGV! (Hiện có: {count} suất chiếu)";
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        #endregion

        #region ===================== CRUD ƯU ĐÃI & TIN TỨC (PROMOS) =====================

        [HttpGet]
        public async Task<IActionResult> GetPromo(int id)
        {
            var promo = await _context.NewsOffers.FindAsync(id);
            if (promo == null) return NotFound();
            return Json(promo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromo(NewsOffer promo)
        {
            promo.CategoryLabel = promo.Category switch
            {
                "offers" => "ƯU ĐÃI",
                "news" => "TIN MỚI",
                "member" => "THÀNH VIÊN",
                "event" => "SỰ KIỆN",
                _ => "ƯU ĐÃI"
            };

            if (string.IsNullOrWhiteSpace(promo.ImageUrl))
            {
                promo.ImageUrl = "https://images.unsplash.com/photo-1568702846914-96b305d2aaeb?w=600";
            }

            _context.NewsOffers.Add(promo);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tạo ưu đãi \"{promo.Title}\" thành công!";
            return RedirectToAction("Index", new { tab = "promos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromo(NewsOffer promo)
        {
            var existing = await _context.NewsOffers.FindAsync(promo.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ưu đãi cần sửa.";
                return RedirectToAction("Index", new { tab = "promos" });
            }

            existing.Title = promo.Title;
            existing.Category = promo.Category;
            existing.CategoryLabel = promo.Category switch
            {
                "offers" => "ƯU ĐÃI",
                "news" => "TIN MỚI",
                "member" => "THÀNH VIÊN",
                "event" => "SỰ KIỆN",
                _ => "ƯU ĐÃI"
            };
            existing.DateRange = promo.DateRange;
            existing.ImageUrl = promo.ImageUrl;
            existing.Summary = promo.Summary;
            existing.Content = promo.Content;
            existing.IsActive = promo.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật ưu đãi \"{promo.Title}\"!";
            return RedirectToAction("Index", new { tab = "promos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromo(int id)
        {
            var promo = await _context.NewsOffers.FindAsync(id);
            if (promo != null)
            {
                _context.NewsOffers.Remove(promo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa ưu đãi!";
            }
            return RedirectToAction("Index", new { tab = "promos" });
        }

        #endregion

        #region ===================== QUẢN LÝ TIẾP NHẬN TT & THUÊ RẠP =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInquiryStatus(int id, string status, string? adminNotes)
        {
            var inquiry = await _context.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.Status = status;
                if (!string.IsNullOrWhiteSpace(adminNotes))
                {
                    inquiry.AdminNotes = adminNotes;
                }
                await _context.SaveChangesAsync();
                await LogActionAsync("Xử lý", "Tiếp nhận TT", $"Cập nhật trạng thái thư liên hệ của {inquiry.FullName} thành \"{status}\"", id.ToString());
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái liên hệ của {inquiry.FullName} thành \"{status}\"!";
            }
            return RedirectToAction("Index", new { tab = "inquiries" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var inquiry = await _context.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                var name = inquiry.FullName;
                _context.ContactInquiries.Remove(inquiry);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Tiếp nhận TT", $"Xóa thư liên hệ của {name} (ID: {id})", id.ToString());
                TempData["SuccessMessage"] = "Đã xóa thư liên hệ!";
            }
            return RedirectToAction("Index", new { tab = "inquiries" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRentalStatus(int id, string status)
        {
            var rental = await _context.HallRentalRequests.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);
            if (rental != null)
            {
                rental.Status = status;
                await _context.SaveChangesAsync();
                await LogActionAsync("Xử lý", "Thuê rạp & Sự kiện", $"Cập nhật trạng thái yêu cầu thuê rạp của {rental.FullName} ({rental.Cinema?.Name ?? "CGV"}) thành \"{status}\"", id.ToString(), rental.CinemaId);
                TempData["SuccessMessage"] = $"Đã cập nhật yêu cầu thuê rạp của {rental.FullName}!";
            }
            return RedirectToAction("Index", new { tab = "rentals" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRental(int id)
        {
            var rental = await _context.HallRentalRequests.FindAsync(id);
            if (rental != null)
            {
                var name = rental.FullName;
                var cinemaId = rental.CinemaId;
                _context.HallRentalRequests.Remove(rental);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Thuê rạp & Sự kiện", $"Xóa yêu cầu thuê rạp của {name} (ID: {id})", id.ToString(), cinemaId);
                TempData["SuccessMessage"] = "Đã xóa yêu cầu thuê rạp!";
            }
            return RedirectToAction("Index", new { tab = "rentals" });
        }

        #endregion

        #region ===================== QUẢN LÝ ĐẶT VÉ (BOOKINGS) =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status, string? returnTab = "bookings")
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                var oldStatus = booking.Status;
                booking.Status = status;
                await _context.SaveChangesAsync();

                int? cinemaId = booking.Showtime?.CinemaHall?.CinemaId;
                await LogActionAsync("Đổi trạng thái", "Đơn vé & Giao dịch",
                    $"Cập nhật trạng thái đơn vé '{booking.BookingCode}' từ '{oldStatus}' thành '{status}' (Khách: {booking.CustomerName}, Tổng tiền: {booking.TotalAmount:N0}đ)",
                    booking.BookingCode, cinemaId);

                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn #{booking.BookingCode} thành \"{status}\"!";
            }
            return RedirectToAction("Index", new { tab = string.IsNullOrWhiteSpace(returnTab) ? "bookings" : returnTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(int id, string? returnTab = "bookings")
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                var code = booking.BookingCode;
                int? cinemaId = booking.Showtime?.CinemaHall?.CinemaId;
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                await LogActionAsync("Xóa", "Đơn vé & Giao dịch", $"Xóa đơn đặt vé #{code} (Khách: {booking.CustomerName}, Tiền: {booking.TotalAmount:N0}đ)", code, cinemaId);
                TempData["SuccessMessage"] = $"Đã xóa đơn đặt vé #{code}!";
            }
            return RedirectToAction("Index", new { tab = string.IsNullOrWhiteSpace(returnTab) ? "bookings" : returnTab });
        }

        #endregion

        #region ===================== CRUD F&B (BẮP NƯỚC & COMBO) =====================

        [HttpGet]
        public async Task<IActionResult> GetFoodItem(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item == null) return NotFound();
            return Json(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFoodItem(FoodItem foodItem)
        {
            if (string.IsNullOrWhiteSpace(foodItem.ImageUrl))
            {
                foodItem.ImageUrl = foodItem.Category switch
                {
                    "Combo" => "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                    "Bắp rang" => "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500",
                    "Nước uống" => "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500",
                    "Snack" => "https://images.unsplash.com/photo-1619740455993-9e612b1af08a?w=500",
                    _ => "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500"
                };
            }

            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();
            await LogActionAsync("Thêm mới", "Bắp nước & F&B", $"Thêm món F&B mới \"{foodItem.Name}\" ({foodItem.Price:N0} đ)", foodItem.Id.ToString());
            TempData["SuccessMessage"] = $"Đã thêm món F&B \"{foodItem.Name}\" thành công!";
            return RedirectToAction("Index", new { tab = "food" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFoodItem(FoodItem foodItem)
        {
            var existing = await _context.FoodItems.FindAsync(foodItem.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy món F&B cần sửa.";
                return RedirectToAction("Index", new { tab = "food" });
            }

            existing.Name = foodItem.Name;
            existing.Category = foodItem.Category;
            existing.Price = foodItem.Price;
            existing.ImageUrl = foodItem.ImageUrl;
            existing.Description = foodItem.Description;
            existing.IsAvailable = foodItem.IsAvailable;
            existing.SortOrder = foodItem.SortOrder;

            await _context.SaveChangesAsync();
            await LogActionAsync("Cập nhật", "Bắp nước & F&B", $"Cập nhật món F&B \"{foodItem.Name}\" ({foodItem.Price:N0} đ)", foodItem.Id.ToString());
            TempData["SuccessMessage"] = $"Đã cập nhật món \"{foodItem.Name}\"!";
            return RedirectToAction("Index", new { tab = "food" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item != null)
            {
                var name = item.Name;
                _context.FoodItems.Remove(item);
                await _context.SaveChangesAsync();
                await LogActionAsync("Xóa", "Bắp nước & F&B", $"Xóa món \"{name}\" khỏi menu F&B", id.ToString());
                TempData["SuccessMessage"] = $"Đã xóa món \"{name}\" khỏi menu F&B!";
            }
            return RedirectToAction("Index", new { tab = "food" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFoodAvailability(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item != null)
            {
                item.IsAvailable = !item.IsAvailable;
                await _context.SaveChangesAsync();
                await LogActionAsync("Cập nhật", "Bắp nước & F&B", $"Đổi trạng thái phục vụ của món \"{item.Name}\" thành {(item.IsAvailable ? "Đang kinh doanh" : "Tạm ngưng")}", id.ToString());
                TempData["SuccessMessage"] = $"Đã đổi trạng thái món \"{item.Name}\" thành {(item.IsAvailable ? "Đang kinh doanh" : "Tạm ngưng")}!";
            }
            return RedirectToAction("Index", new { tab = "food" });
        }

        #endregion
    }
}
