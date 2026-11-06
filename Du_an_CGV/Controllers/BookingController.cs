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

            // Dọn dẹp các ghế giữ chỗ đã quá 5 phút
            await CleanExpiredHoldsAsync();

            // Find occupied seats and held seats for this showtime
            var occupiedSeats = new List<string>();
            var heldByOtherSeats = new List<string>();
            var myHeldSeats = new List<string>();
            int myRemainingSeconds = 0;

            var currentUser = (User.Identity != null && User.Identity.IsAuthenticated)
                ? await _userManager.GetUserAsync(User)
                : null;

            if (selectedShowtime != null)
            {
                // Ghế đã thanh toán chính thức
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

                // Ghế đang giữ chỗ 5 phút (SeatHolds / GIUCHO)
                var activeHolds = await _context.SeatHolds
                    .Where(h => h.ShowtimeId == selectedShowtime.Id && h.ExpiresAt > DateTime.Now)
                    .ToListAsync();

                foreach (var hold in activeHolds)
                {
                    if (currentUser != null && hold.UserId == currentUser.Id)
                    {
                        myHeldSeats.Add(hold.SeatCode);
                        var rem = (int)(hold.ExpiresAt - DateTime.Now).TotalSeconds;
                        if (rem > myRemainingSeconds) myRemainingSeconds = rem;
                    }
                    else
                    {
                        heldByOtherSeats.Add(hold.SeatCode);
                    }
                }
            }

            // Tính ưu đãi cấp bậc thành viên CGV
            decimal memberDiscountPercent = 0;
            string memberTierName = "Thành viên Thường";
            string memberBadgeColor = "#607d8b";
            if (currentUser != null)
            {
                var tier = await _context.MemberTiers.FirstOrDefaultAsync(t => t.Id == currentUser.MemberTierId)
                           ?? await _context.MemberTiers.OrderByDescending(t => t.MinPoints).FirstOrDefaultAsync(t => currentUser.RewardPoints >= t.MinPoints);
                if (tier != null)
                {
                    memberDiscountPercent = tier.DiscountPercent;
                    memberTierName = tier.TierName;
                    memberBadgeColor = tier.BadgeColor;
                }
            }

            ViewBag.Movies = movies;
            ViewBag.SelectedMovie = selectedMovie;
            ViewBag.Cinemas = cinemas;
            ViewBag.SelectedCinema = selectedCinema;
            ViewBag.Showtimes = showtimes;
            ViewBag.SelectedShowtime = selectedShowtime;
            ViewBag.OccupiedSeats = occupiedSeats.Distinct().ToList();
            ViewBag.HeldByOtherSeats = heldByOtherSeats.Distinct().ToList();
            ViewBag.MyHeldSeats = myHeldSeats.Distinct().ToList();
            ViewBag.MyRemainingSeconds = myRemainingSeconds;
            ViewBag.MemberDiscountPercent = memberDiscountPercent;
            ViewBag.MemberTierName = memberTierName;
            ViewBag.MemberBadgeColor = memberBadgeColor;

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

        private async Task CleanExpiredHoldsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var expiredHolds = await _context.SeatHolds
                    .Where(h => h.ExpiresAt <= now)
                    .ToListAsync();
                if (expiredHolds.Any())
                {
                    _context.SeatHolds.RemoveRange(expiredHolds);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Silent fail
            }
        }

        // POST: /Booking/HoldSeats (AJAX - Giữ chỗ trong 5 phút)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> HoldSeats(int showtimeId, string seatsList)
        {
            await CleanExpiredHoldsAsync();

            if (string.IsNullOrWhiteSpace(seatsList))
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 ghế ngồi!" });
            }

            var showtime = await _context.Showtimes.FindAsync(showtimeId);
            if (showtime == null)
            {
                return Json(new { success = false, message = "Suất chiếu không tồn tại hoặc đã bị hủy!" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập tài khoản thành viên để giữ chỗ!" });
            }

            var requestedSeats = seatsList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // 1. Kiểm tra ghế đã có người thanh toán chính thức chưa
            var bookings = await _context.Bookings
                .Where(b => b.ShowtimeId == showtimeId && b.Status != "Đã hủy")
                .Select(b => b.SeatsList)
                .ToListAsync();

            var bookedSeatSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in bookings)
            {
                var cleanSeatPart = b.Split('(')[0];
                foreach (var s in cleanSeatPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    bookedSeatSet.Add(s);
                }
            }

            foreach (var seat in requestedSeats)
            {
                if (bookedSeatSet.Contains(seat))
                {
                    return Json(new { success = false, message = $"Ghế {seat} đã có người đặt và thanh toán thành công! Vui lòng chọn ghế khác." });
                }
            }

            // 2. Kiểm tra ghế có đang được KHÁCH HÀNG KHÁC giữ chỗ trong 5 phút hay không
            var now = DateTime.Now;
            var otherHolds = await _context.SeatHolds
                .Where(h => h.ShowtimeId == showtimeId && h.ExpiresAt > now && h.UserId != user.Id)
                .ToListAsync();

            var heldSeatSet = new HashSet<string>(otherHolds.Select(h => h.SeatCode), StringComparer.OrdinalIgnoreCase);
            foreach (var seat in requestedSeats)
            {
                if (heldSeatSet.Contains(seat))
                {
                    return Json(new { success = false, message = $"Ghế {seat} đang được người khác giữ chỗ trong 5 phút! Vui lòng chọn ghế khác." });
                }
            }

            // 3. Xóa các lượt giữ chỗ cũ của người dùng này cho cùng suất chiếu
            var myOldHolds = await _context.SeatHolds
                .Where(h => h.ShowtimeId == showtimeId && h.UserId == user.Id)
                .ToListAsync();
            _context.SeatHolds.RemoveRange(myOldHolds);

            // 4. Tạo bản ghi giữ chỗ mới trong đúng 5 phút (GIUCHO)
            var expiresAt = now.AddMinutes(5);
            foreach (var seat in requestedSeats)
            {
                _context.SeatHolds.Add(new SeatHold
                {
                    ShowtimeId = showtimeId,
                    SeatCode = seat,
                    UserId = user.Id,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Hệ thống đã giữ chỗ thành công trong 5 phút!",
                expiresAt = expiresAt.ToString("o"),
                remainingSeconds = 300
            });
        }

        // POST: /Booking/ReleaseHolds (AJAX - Hủy giữ chỗ khi khách đổi ý hoặc đóng modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ReleaseHolds(int showtimeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var myHolds = await _context.SeatHolds
                    .Where(h => h.ShowtimeId == showtimeId && h.UserId == user.Id)
                    .ToListAsync();
                if (myHolds.Any())
                {
                    _context.SeatHolds.RemoveRange(myHolds);
                    await _context.SaveChangesAsync();
                }
            }
            return Json(new { success = true });
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản người dùng!";
                return RedirectToAction("Index", new { showtimeId });
            }

            var seatArray = seatsList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (seatArray.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ghế ngồi hợp lệ!";
                return RedirectToAction("Index", new { showtimeId });
            }

            // KIỂM TRA HIỆU LỰC GIỮ CHỖ TRONG 5 PHÚT (GIUCHO)
            var now = DateTime.Now;
            var myHolds = await _context.SeatHolds
                .Where(h => h.ShowtimeId == showtime.Id && h.UserId == user.Id && h.ExpiresAt > now)
                .ToListAsync();

            var myHeldSeatCodes = new HashSet<string>(myHolds.Select(h => h.SeatCode), StringComparer.OrdinalIgnoreCase);
            bool isHoldValid = seatArray.All(s => myHeldSeatCodes.Contains(s));

            if (!isHoldValid)
            {
                TempData["ErrorMessage"] = "Thời gian giữ chỗ 5 phút đã hết hạn! Ghế đã được tự động giải phóng. Vui lòng chọn lại ghế để tiếp tục.";
                return RedirectToAction("Index", new { showtimeId });
            }

            // Tính giá vé + phụ thu hàng ghế VIP
            decimal seatTotal = 0;
            foreach (var seat in seatArray)
            {
                decimal seatPrice = showtime.Price;
                if (seat.StartsWith("C") || seat.StartsWith("D"))
                {
                    seatPrice += 15000; // VIP row surcharge
                }
                seatTotal += seatPrice;
            }

            // Áp dụng chiết khấu cấp bậc thành viên (CAPBAC_THANHVIEN)
            var userTier = await _context.MemberTiers.FirstOrDefaultAsync(t => t.Id == user.MemberTierId)
                           ?? await _context.MemberTiers.OrderByDescending(t => t.MinPoints).FirstOrDefaultAsync(t => user.RewardPoints >= t.MinPoints);

            decimal discountAmount = 0;
            if (userTier != null && userTier.DiscountPercent > 0)
            {
                discountAmount = Math.Round(seatTotal * (userTier.DiscountPercent / 100m), 0);
            }

            decimal totalAmount = seatTotal - discountAmount;
            if (foodTotal > 0)
            {
                totalAmount += foodTotal;
            }

            // Vietnam local time code
            var code = $"CGV-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var fullSeatsDescription = string.IsNullOrWhiteSpace(foodItemsSummary) 
                ? seatsList 
                : $"{seatsList} (+ Bắp nước: {foodItemsSummary})";

            // TẠO ĐƠN VÉ CHÍNH THỨC VÀ ĐÁNH DẤU LÀ "ĐÃ THANH TOÁN"
            // Khi thanh toán xong thì mới tính ghế đó thuộc về mình
            var booking = new Booking
            {
                BookingCode = code,
                UserId = user.Id,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? (user.FullName ?? "Khách Hàng CGV") : customerName.Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? (user.Email ?? "khachhang@cgv.vn") : customerEmail.Trim(),
                CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? (user.PhoneNumber ?? "19006017") : customerPhone.Trim(),
                ShowtimeId = showtime.Id,
                BookingDate = now,
                SeatsList = fullSeatsDescription,
                TotalAmount = totalAmount,
                Status = "Đã thanh toán",
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "VNPAY-QR" : paymentMethod
            };

            _context.Bookings.Add(booking);

            // Xóa bản ghi giữ chỗ tạm thời vì đã chuyển thành vé thanh toán chính thức
            _context.SeatHolds.RemoveRange(myHolds);

            // Tích điểm thưởng cho thành viên (1 điểm cho mỗi 1.000 VNĐ chi tiêu)
            int earnedPoints = (int)(totalAmount / 1000);
            user.RewardPoints += earnedPoints;

            // Tự động thăng cấp bậc thành viên nếu đủ điểm yêu cầu
            var allTiers = await _context.MemberTiers.OrderByDescending(t => t.MinPoints).ToListAsync();
            var qualifyingTier = allTiers.FirstOrDefault(t => user.RewardPoints >= t.MinPoints);
            if (qualifyingTier != null && qualifyingTier.Id != user.MemberTierId)
            {
                user.MemberTierId = qualifyingTier.Id;
                user.MemberTier = qualifyingTier.TierName;
                TempData["TierUpgradeMessage"] = $"🎉 Chúc mừng bạn đã được nâng hạng thành viên CGV: {qualifyingTier.TierName} ({qualifyingTier.DiscountPercent}% ưu đãi vé)!";
            }

            await _userManager.UpdateAsync(user);
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

