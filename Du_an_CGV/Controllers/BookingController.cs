using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace Du_an_CGV.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Booking?movieId=1&cinemaId=1&showtimeId=1
        public async Task<IActionResult> Index(int? movieId, int? cinemaId, int? showtimeId)
        {
            // Bắt buộc đăng nhập trước khi mua vé / đặt vé
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Index", "Booking", new { movieId, cinemaId, showtimeId });
                TempData["WarningMessage"] = "Vui lòng đăng nhập tài khoản thành viên để mua vé & đặt vé xem phim!";
                return RedirectToAction("Register", "Account", new { tab = "login", returnUrl });
            }

            // 1. If showtimeId is provided, resolve movie and cinema from that showtime first
            Showtime? targetShowtime = null;
            if (showtimeId.HasValue && showtimeId.Value > 0)
            {
                targetShowtime = await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                    .FirstOrDefaultAsync(s => s.Id == showtimeId.Value);

                if (targetShowtime != null)
                {
                    movieId = targetShowtime.MovieId;
                    cinemaId = targetShowtime.CinemaHall?.CinemaId;
                }
            }

            var movies = await _context.Movies
                .Where(m => m.IsNowShowing && !m.IsUpcoming)
                .OrderBy(m => m.Title)
                .ToListAsync();

            Movie? selectedMovie = null;
            if (movieId.HasValue && movieId.Value > 0)
            {
                var checkMovie = await _context.Movies.FindAsync(movieId.Value);
                if (checkMovie != null && (checkMovie.IsUpcoming || !checkMovie.IsNowShowing))
                {
                    TempData["ErrorMessage"] = $"Phim \"{checkMovie.Title}\" hiện đang ở trạng thái Sắp chiếu (chưa mở bán vé)!";
                    return RedirectToAction("Detail", "Movie", new { id = checkMovie.Id });
                }
                selectedMovie = movies.FirstOrDefault(m => m.Id == movieId.Value);
            }

            if (selectedMovie == null)
            {
                var movieWithShowtimesId = await _context.Showtimes
                    .Select(s => s.MovieId)
                    .FirstOrDefaultAsync();

                if (movieWithShowtimesId > 0)
                {
                    selectedMovie = movies.FirstOrDefault(m => m.Id == movieWithShowtimesId);
                }

                if (selectedMovie == null)
                {
                    selectedMovie = movies.FirstOrDefault();
                }
            }

            var cinemas = await _context.Cinemas
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();

            Cinema? selectedCinema = null;
            if (cinemaId.HasValue && cinemaId.Value > 0)
            {
                selectedCinema = cinemas.FirstOrDefault(c => c.Id == cinemaId.Value);
            }

            // 2. If no cinema selected or selected cinema has 0 showtimes for selectedMovie,
            // find a cinema that ACTUALLY has showtimes for this movie
            if (selectedMovie != null)
            {
                bool currentHasShowtimes = false;
                if (selectedCinema != null)
                {
                    currentHasShowtimes = await _context.Showtimes
                        .AnyAsync(s => s.MovieId == selectedMovie.Id && s.CinemaHall!.CinemaId == selectedCinema.Id);
                }

                if (!currentHasShowtimes)
                {
                    var availableSt = await _context.Showtimes
                        .Include(s => s.CinemaHall)
                            .ThenInclude(h => h!.Cinema)
                        .Where(s => s.MovieId == selectedMovie.Id)
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefaultAsync();

                    if (availableSt?.CinemaHall?.Cinema != null)
                    {
                        selectedCinema = cinemas.FirstOrDefault(c => c.Id == availableSt.CinemaHall.CinemaId) 
                                         ?? availableSt.CinemaHall.Cinema;
                    }
                }
            }

            if (selectedCinema == null)
            {
                selectedCinema = cinemas.FirstOrDefault();
            }

            // 3. Load showtimes for (selectedMovie, selectedCinema)
            var showtimesQuery = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .AsQueryable();

            if (selectedMovie != null)
            {
                showtimesQuery = showtimesQuery.Where(s => s.MovieId == selectedMovie.Id);
            }

            if (selectedCinema != null)
            {
                showtimesQuery = showtimesQuery.Where(s => s.CinemaHall!.CinemaId == selectedCinema.Id);
            }

            var showtimes = await showtimesQuery
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // Fallback: If still no showtimes for this cinema, check if any showtime exists for this movie anywhere
            if (!showtimes.Any() && selectedMovie != null)
            {
                showtimes = await _context.Showtimes
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                    .Where(s => s.MovieId == selectedMovie.Id)
                    .OrderBy(s => s.StartTime)
                    .Take(10)
                    .ToListAsync();

                if (showtimes.Any())
                {
                    selectedCinema = showtimes.First().CinemaHall?.Cinema ?? selectedCinema;
                }
            }

            var selectedShowtime = targetShowtime;
            if (selectedShowtime == null || selectedShowtime.MovieId != selectedMovie?.Id || (selectedCinema != null && selectedShowtime.CinemaHall?.CinemaId != selectedCinema.Id))
            {
                selectedShowtime = showtimeId.HasValue
                    ? showtimes.FirstOrDefault(s => s.Id == showtimeId.Value) ?? showtimes.FirstOrDefault()
                    : showtimes.FirstOrDefault();
            }

            // Find occupied seats for this showtime
            var occupiedSeats = new List<string>();
            if (selectedShowtime != null)
            {
                var bookings = await _context.Bookings
                    .Where(b => b.ShowtimeId == selectedShowtime.Id && b.Status != "Đã hủy")
                    .Select(b => b.SeatsList)
                    .ToListAsync();

                foreach (var seatStr in bookings)
                {
                    // Filter out food notation if present in seat string
                    var cleanSeatPart = seatStr.Split('(')[0];
                    var split = cleanSeatPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    occupiedSeats.AddRange(split);
                }
            }

            ViewBag.Movies = movies;
            ViewBag.SelectedMovie = selectedMovie;
            ViewBag.Cinemas = cinemas;
            ViewBag.SelectedCinema = selectedCinema;
            ViewBag.Showtimes = showtimes;
            ViewBag.SelectedShowtime = selectedShowtime;
            ViewBag.OccupiedSeats = occupiedSeats.Distinct().ToList();

            var foodItems = await _context.FoodItems
                .Where(f => f.IsAvailable)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .ToListAsync();
            ViewBag.FoodItems = foodItems;

            // Pre-fill user information if logged in
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserFullName = user.FullName;
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserPhone = user.PhoneNumber;
                }
            }

            return View(selectedMovie);
        }

        // POST: /Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmBooking(
            int showtimeId,
            string seatsList,
            string customerName,
            string customerEmail,
            string customerPhone,
            string paymentMethod,
            string? foodItemsSummary = null,
            decimal foodTotal = 0,
            string? foodItemsJson = null)
        {
            // Bắt buộc đăng nhập trước khi xác nhận đặt vé
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                TempData["WarningMessage"] = "Vui lòng đăng nhập tài khoản thành viên để hoàn tất đặt vé!";
                return RedirectToAction("Register", "Account", new { tab = "login" });
            }

            if (string.IsNullOrWhiteSpace(seatsList))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ghế ngồi!";
                return RedirectToAction("Index", new { showtimeId });
            }

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(s => s.Id == showtimeId);

            if (showtime == null)
            {
                TempData["ErrorMessage"] = "Suất chiếu không tồn tại hoặc đã bị hủy. Vui lòng chọn suất chiếu khác!";
                return RedirectToAction("Index");
            }

            if (showtime.Movie != null && (showtime.Movie.IsUpcoming || !showtime.Movie.IsNowShowing))
            {
                TempData["ErrorMessage"] = "Phim này hiện ở trạng thái Sắp chiếu hoặc đã Ngưng chiếu, chưa thể đặt vé!";
                return RedirectToAction("Index");
            }

            var seatArray = seatsList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (seatArray.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ghế ngồi hợp lệ!";
                return RedirectToAction("Index", new { showtimeId });
            }

            // Seat price calculation + VIP surcharge
            decimal totalAmount = 0;
            foreach (var seat in seatArray)
            {
                decimal seatPrice = showtime.Price;
                if (seat.StartsWith("C") || seat.StartsWith("D"))
                {
                    seatPrice += 15000; // VIP row surcharge
                }
                totalAmount += seatPrice;
            }

            if (foodTotal > 0)
            {
                totalAmount += foodTotal;
            }

            // Vietnam local time code
            var now = DateTime.Now;
            var code = $"CGV-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var user = await _userManager.GetUserAsync(User);
            string? userId = user?.Id;
            if (user != null)
            {
                user.RewardPoints += (int)(totalAmount / 1000); // 1 point per 1000 VND
                await _userManager.UpdateAsync(user);
            }

            var fullSeatsDescription = string.IsNullOrWhiteSpace(foodItemsSummary) 
                ? seatsList 
                : $"{seatsList} (+ Bắp nước: {foodItemsSummary})";

            var booking = new Booking
            {
                BookingCode = code,
                UserId = userId,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? (user?.FullName ?? "Khách Hàng CGV") : customerName.Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? (user?.Email ?? "khachhang@cgv.vn") : customerEmail.Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? (user?.PhoneNumber ?? "19006017") : customerPhone.Trim(),
                ShowtimeId = showtime.Id,
                BookingDate = now,
                SeatsList = fullSeatsDescription,
                TotalAmount = totalAmount,
                Status = "Đã thanh toán",
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "VNPAY-QR" : paymentMethod
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            try
            {
                var cinema = showtime.CinemaHall?.Cinema;
                _context.AdminActionLogs.Add(new AdminActionLog
                {
                    ActionType = "Giao dịch vé",
                    ModuleName = "Đơn vé & Giao dịch",
                    Description = $"Khách hàng {booking.CustomerName} hoàn tất thanh toán vé {booking.BookingCode} ({booking.TotalAmount:N0} đ) qua {booking.PaymentMethod} tại {cinema?.Name ?? "CGV"}",
                    EntityId = booking.BookingCode,
                    PerformedBy = "Cổng thanh toán " + booking.PaymentMethod,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    CinemaId = cinema?.Id,
                    City = cinema?.City,
                    CreatedAt = DateTime.Now,
                    Status = "Thành công"
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silent fail
            }

            // Lưu các bản ghi liên kết chi tiết bắp nước (BookingFoodItem)
            if (!string.IsNullOrWhiteSpace(foodItemsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<BookingFoodItemDto>>(
                        foodItemsJson, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (items != null && items.Any())
                    {
                        foreach (var item in items)
                        {
                            if (item.Quantity > 0 && item.FoodItemId > 0)
                            {
                                _context.BookingFoodItems.Add(new BookingFoodItem
                                {
                                    BookingId = booking.Id,
                                    FoodItemId = item.FoodItemId,
                                    Quantity = item.Quantity,
                                    UnitPrice = item.UnitPrice
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            // Xóa cookie cũ để không ép lọc 1 vé
            Response.Cookies.Delete("CGV_LastBookingCode");

            TempData["SuccessMessage"] = $"Đặt vé thành công! Mã vé của bạn là: {booking.BookingCode}. Hãy xuất trình mã QR tại rạp chiếu.";
            TempData["LastBookingCode"] = booking.BookingCode;

            // Direct to "Vé Của Tôi" page showing all user tickets
            return RedirectToAction("MyTickets");
        }

        // GET: /Booking/MyTickets
        public async Task<IActionResult> MyTickets(string? search = null)
        {
            var query = _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .AsQueryable();

            // Mã vé vừa đặt xong (dùng để highlight vé mới nhất ở đầu danh sách)
            var lastCode = (string?)TempData["LastBookingCode"];

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (!User.IsInRole("Admin"))
                    {
                        // Khách hàng đã đăng nhập: luôn lấy toàn bộ vé của tài khoản
                        query = query.Where(b => b.UserId == user.Id || b.CustomerEmail == user.Email);
                    }
                }
            }
            else
            {
                // Khách chưa đăng nhập: chỉ xem nếu có nhập mã tra cứu
                if (string.IsNullOrWhiteSpace(search))
                {
                    ViewBag.SearchTerm = "";
                    ViewBag.IsGuestFirstVisit = true;
                    return View(new List<Booking>());
                }
            }

            // Chỉ lọc khi người dùng chủ động gõ từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(b => b.BookingCode.Contains(term) 
                                      || b.CustomerPhone.Contains(term) 
                                      || b.CustomerEmail.Contains(term));
            }

            var tickets = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.SearchTerm = search ?? "";
            ViewBag.LastBookingCode = lastCode;

            return View(tickets);
        }
    }

    public class BookingFoodItemDto
    {
        public int FoodItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

