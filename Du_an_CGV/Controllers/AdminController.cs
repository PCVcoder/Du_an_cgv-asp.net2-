using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Du_an_CGV.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    [Authorize(Roles = "Admin")]
    public partial class AdminController : Controller
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
                .AsNoTracking()
                .OrderByDescending(m => m.Id)
                .ToListAsync();

            var cinemas = await _context.Cinemas
                .AsNoTracking()
                .Include(c => c.CinemaHalls)
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var today = DateTime.Today;
            var showtimes = await _context.Showtimes
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.StartTime >= today.AddDays(-1))
                .OrderBy(s => s.StartTime)
                .Take(500)
                .ToListAsync();

            if (!showtimes.Any())
            {
                showtimes = await _context.Showtimes
                    .AsNoTracking()
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                    .OrderByDescending(s => s.StartTime)
                    .Take(300)
                    .ToListAsync();
            }

            // Tính toán xung đột lịch chiếu theo từng phòng một cách tối ưu (O(N) thay vì O(N^2))
            var conflictShowtimeIds = new HashSet<int>();
            var conflictDetails = new Dictionary<int, string>();
            var showtimesByHall = showtimes.GroupBy(s => s.CinemaHallId);
            foreach (var hallGroup in showtimesByHall)
            {
                var list = hallGroup.OrderBy(s => s.StartTime).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        if (list[j].StartTime < list[i].EndTime)
                        {
                            conflictShowtimeIds.Add(list[i].Id);
                            conflictShowtimeIds.Add(list[j].Id);
                            var hallName = list[i].CinemaHall?.Name ?? "phòng";
                            conflictDetails[list[i].Id] = $"Cùng {hallName} với \"{list[j].Movie?.Title}\" ({list[j].StartTime:HH:mm} - {list[j].EndTime:HH:mm})";
                            conflictDetails[list[j].Id] = $"Cùng {hallName} với \"{list[i].Movie?.Title}\" ({list[i].StartTime:HH:mm} - {list[i].EndTime:HH:mm})";
                        }
                        else
                        {
                            break; // Do đã sắp xếp theo StartTime nên các suất sau sẽ bắt đầu muộn hơn nữa
                        }
                    }
                }
            }

            var promos = await _context.NewsOffers
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var inquiries = await _context.ContactInquiries
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var rentals = await _context.HallRentalRequests
                .AsNoTracking()
                .Include(r => r.Cinema)
                    .ThenInclude(c => c!.CinemaHalls)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            foreach (var r in rentals)
            {
                if (r.Cinema == null && !string.IsNullOrEmpty(r.CinemaName))
                {
                    r.Cinema = cinemas.FirstOrDefault(c => c.Name == r.CinemaName);
                }
            }

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .OrderByDescending(b => b.BookingDate)
                .Take(50)
                .ToListAsync();

            var allBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .OrderByDescending(b => b.BookingDate)
                .Take(150)
                .ToListAsync();

            var actionLogs = await _context.AdminActionLogs
                .AsNoTracking()
                .Include(l => l.Cinema)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            var foodItems = await _context.FoodItems
                .AsNoTracking()
                .OrderBy(f => f.Category)
                .ThenBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .ToListAsync();

            var totalRevenue = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.Status == "Đã thanh toán")
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            var provinces = cinemas
                .Select(c => c.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var memberTiers = await _context.MemberTiers
                .AsNoTracking()
                .OrderBy(t => t.MinPoints)
                .ToListAsync();

            var now = DateTime.Now;
            var activeSeatHolds = await _context.SeatHolds
                .AsNoTracking()
                .Include(h => h.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(h => h.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(ch => ch!.Cinema)
                .Include(h => h.User)
                .Where(h => h.ExpiresAt > now)
                .OrderByDescending(h => h.ExpiresAt)
                .ToListAsync();

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
                Provinces = provinces,
                MemberTiers = memberTiers,
                ActiveSeatHolds = activeSeatHolds,
                ConflictShowtimeIds = conflictShowtimeIds,
                ConflictDetails = conflictDetails
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

        /*
         * =========================================================================================
         * CÁC MODULE CHỨC NĂNG ADMIN ĐÃ ĐƯỢC TÁCH THÀNH CÁC TỆP PARTIAL CLASS RIÊNG BIỆT DỄ QUẢN LÝ:
         * =========================================================================================
         * - AdminController.Movies.cs           : Quản lý Phim (CRUD Phim)
         * - AdminController.Cinemas.cs          : Quản lý Cụm Rạp (CRUD Cụm Rạp)
         * - AdminController.Halls.cs            : Quản lý Phòng Chiếu & Cấu hình Ma Trận Sơ Đồ Ghế
         * - AdminController.Showtimes.cs        : Quản lý Suất Chiếu (CRUD Suất Chiếu & Kiểm tra trùng lịch)
         * - AdminController.Promos.cs           : Quản lý Ưu Đãi & Tin Tức (CRUD Tin Tức & Khuyến Mãi)
         * - AdminController.RentalRequests.cs   : Tiếp Nhận Phản Hồi & Yêu Cầu Thuê Rạp / Sự Kiện
         * - AdminController.Bookings.cs         : Quản Lý Đặt Vé & Giao Dịch
         * - AdminController.FoodBeverage.cs     : Quản Lý Bắp Nước & Combo F&B
         * - AdminController.Reports.cs          : Báo Cáo & Thống Kê Doanh Thu Realtime & Xuất Excel/CSV
         * =========================================================================================
         */
    }
}
