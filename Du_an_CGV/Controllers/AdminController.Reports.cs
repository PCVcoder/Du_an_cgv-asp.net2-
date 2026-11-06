using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
        #region BÁO CÁO & THỐNG KÊ DOANH THU

        [HttpGet]
        public IActionResult Reports()
        {
            return RedirectToAction("Index", new { tab = "reports" });
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueReportData(DateTime? fromDate, DateTime? toDate, int? cinemaId, int? movieId)
        {
            var today = DateTime.Today;
            var start = fromDate?.Date ?? today.AddDays(-29);
            var end = (toDate?.Date ?? today).AddDays(1).AddTicks(-1);

            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .Include(b => b.BookingFoodItems)
                    .ThenInclude(bf => bf.FoodItem)
                .Where(b => b.Status == "Đã thanh toán" && !b.IsDeleted && b.BookingDate >= start && b.BookingDate <= end);

            if (cinemaId.HasValue && cinemaId.Value > 0)
            {
                query = query.Where(b => b.Showtime != null && b.Showtime.CinemaHall != null && b.Showtime.CinemaHall.CinemaId == cinemaId.Value);
            }

            if (movieId.HasValue && movieId.Value > 0)
            {
                query = query.Where(b => b.Showtime != null && b.Showtime.MovieId == movieId.Value);
            }

            var bookings = await query.OrderByDescending(b => b.BookingDate).ToListAsync();

            // 3a. Luồng ngoại lệ: Không có dữ liệu trong khoảng thời gian chọn
            if (!bookings.Any())
            {
                return Json(new
                {
                    hasData = false,
                    message = "Không có dữ liệu giao dịch đã thanh toán trong khoảng thời gian đã chọn.",
                    fromDate = start.ToString("yyyy-MM-dd"),
                    toDate = (toDate?.Date ?? today).ToString("yyyy-MM-dd"),
                    summary = new
                    {
                        totalRevenue = 0,
                        totalTicketRevenue = 0,
                        totalFoodRevenue = 0,
                        totalTicketsSold = 0,
                        totalOrders = 0,
                        occupancyRate = 0,
                        avgOrderValue = 0
                    },
                    timeline = new List<object>(),
                    byMovie = new List<object>(),
                    byCinema = new List<object>(),
                    byPayment = new List<object>(),
                    details = new List<object>()
                });
            }

            // 1. Tính toán tóm tắt tổng thể
            decimal totalRevenue = bookings.Sum(b => b.TotalAmount);
            decimal totalFoodRevenue = bookings.Sum(b => b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0);
            decimal totalTicketRevenue = bookings.Sum(b => {
                var food = b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0;
                return b.TotalAmount > food ? b.TotalAmount - food : b.TotalAmount;
            });
            int totalTicketsSold = bookings.Sum(b => !string.IsNullOrWhiteSpace(b.SeatsList) 
                ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length 
                : 1);
            int totalOrders = bookings.Count;
            decimal avgOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 0) : 0;

            // 2. Tính Tỷ lệ lấp đầy ghế (Occupancy Rate)
            var showtimeQuery = _context.Showtimes
                .AsNoTracking()
                .Include(s => s.CinemaHall)
                .Where(s => !s.IsDeleted && s.StartTime >= start && s.StartTime <= end);

            if (cinemaId.HasValue && cinemaId.Value > 0)
            {
                showtimeQuery = showtimeQuery.Where(s => s.CinemaHall != null && s.CinemaHall.CinemaId == cinemaId.Value);
            }
            if (movieId.HasValue && movieId.Value > 0)
            {
                showtimeQuery = showtimeQuery.Where(s => s.MovieId == movieId.Value);
            }

            var matchingShowtimes = await showtimeQuery.ToListAsync();
            int totalCapacity = matchingShowtimes.Sum(s => s.CinemaHall?.TotalSeats > 0 ? s.CinemaHall.TotalSeats : 96);
            decimal occupancyRate = 0;
            if (totalCapacity > 0)
            {
                occupancyRate = Math.Round((decimal)totalTicketsSold / totalCapacity * 100, 1);
                if (occupancyRate > 100) occupancyRate = 100;
            }
            else if (totalTicketsSold > 0)
            {
                var bookedShowtimeIds = bookings.Select(b => b.ShowtimeId).Distinct().ToList();
                var bookedHalls = await _context.Showtimes
                    .AsNoTracking()
                    .Include(s => s.CinemaHall)
                    .Where(s => bookedShowtimeIds.Contains(s.Id))
                    .ToListAsync();
                int bookedCapacity = bookedHalls.Sum(s => s.CinemaHall?.TotalSeats > 0 ? s.CinemaHall.TotalSeats : 96);
                if (bookedCapacity > 0)
                {
                    occupancyRate = Math.Round((decimal)totalTicketsSold / bookedCapacity * 100, 1);
                    if (occupancyRate > 100) occupancyRate = 100;
                }
            }

            // 3. Chuỗi thời gian (Timeline) theo ngày cho biểu đồ
            var timeline = bookings
                .GroupBy(b => b.BookingDate.ToString("dd/MM/yyyy"))
                .Select(g => new
                {
                    date = g.Key,
                    dateRaw = g.First().BookingDate.Date,
                    totalRevenue = g.Sum(b => b.TotalAmount),
                    foodRevenue = g.Sum(b => b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0),
                    ticketRevenue = g.Sum(b => {
                        var food = b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0;
                        return b.TotalAmount > food ? b.TotalAmount - food : b.TotalAmount;
                    }),
                    ticketsSold = g.Sum(b => !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1),
                    ordersCount = g.Count()
                })
                .OrderBy(x => x.dateRaw)
                .Select(x => new
                {
                    x.date,
                    x.totalRevenue,
                    x.foodRevenue,
                    x.ticketRevenue,
                    x.ticketsSold,
                    x.ordersCount
                })
                .ToList();

            // 4. Doanh thu theo Phim (By Movie)
            var byMovie = bookings
                .GroupBy(b => b.Showtime?.MovieId ?? 0)
                .Select(g => {
                    var first = g.First();
                    var movieTitle = first.Showtime?.Movie?.Title ?? "Phim khác";
                    var posterUrl = first.Showtime?.Movie?.PosterUrl ?? "";
                    var genre = first.Showtime?.Movie?.Genre ?? "";
                    var rev = g.Sum(b => b.TotalAmount);
                    var foodRev = g.Sum(b => b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0);
                    var ticketRev = g.Sum(b => {
                        var f = b.BookingFoodItems?.Sum(x => (decimal)x.Quantity * x.UnitPrice) ?? 0;
                        return b.TotalAmount > f ? b.TotalAmount - f : b.TotalAmount;
                    });
                    var tix = g.Sum(b => !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1);
                    var share = totalRevenue > 0 ? Math.Round(rev / totalRevenue * 100, 1) : 0;
                    return new
                    {
                        movieId = g.Key,
                        movieTitle,
                        posterUrl,
                        genre,
                        totalRevenue = rev,
                        ticketRevenue = ticketRev,
                        foodRevenue = foodRev,
                        ticketsSold = tix,
                        ordersCount = g.Count(),
                        percentage = share
                    };
                })
                .OrderByDescending(m => m.totalRevenue)
                .ToList();

            // 5. Doanh thu theo Cụm Rạp (By Cinema)
            var byCinema = bookings
                .GroupBy(b => b.Showtime?.CinemaHall?.CinemaId ?? 0)
                .Select(g => {
                    var first = g.First();
                    var cinemaName = first.Showtime?.CinemaHall?.Cinema?.Name ?? "CGV";
                    var city = first.Showtime?.CinemaHall?.Cinema?.City ?? "";
                    var rev = g.Sum(b => b.TotalAmount);
                    var foodRev = g.Sum(b => b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0);
                    var ticketRev = g.Sum(b => {
                        var f = b.BookingFoodItems?.Sum(x => (decimal)x.Quantity * x.UnitPrice) ?? 0;
                        return b.TotalAmount > f ? b.TotalAmount - f : b.TotalAmount;
                    });
                    var tix = g.Sum(b => !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1);
                    var share = totalRevenue > 0 ? Math.Round(rev / totalRevenue * 100, 1) : 0;
                    return new
                    {
                        cinemaId = g.Key,
                        cinemaName,
                        city,
                        totalRevenue = rev,
                        ticketRevenue = ticketRev,
                        foodRevenue = foodRev,
                        ticketsSold = tix,
                        ordersCount = g.Count(),
                        percentage = share
                    };
                })
                .OrderByDescending(c => c.totalRevenue)
                .ToList();

            // 6. Doanh thu theo Cổng thanh toán (By Payment Method)
            var byPayment = bookings
                .GroupBy(b => string.IsNullOrWhiteSpace(b.PaymentMethod) ? "Khác" : b.PaymentMethod)
                .Select(g => new
                {
                    method = g.Key,
                    totalRevenue = g.Sum(b => b.TotalAmount),
                    ordersCount = g.Count(),
                    percentage = totalRevenue > 0 ? Math.Round(g.Sum(b => b.TotalAmount) / totalRevenue * 100, 1) : 0
                })
                .OrderByDescending(p => p.totalRevenue)
                .ToList();

            // 7. Chi tiết từng giao dịch đã thanh toán
            var details = bookings.Take(200).Select(b => {
                var food = b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0;
                var ticket = b.TotalAmount > food ? b.TotalAmount - food : b.TotalAmount;
                var seatCount = !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1;
                var foodDesc = b.BookingFoodItems != null && b.BookingFoodItems.Any()
                    ? string.Join(", ", b.BookingFoodItems.Select(f => $"{f.FoodItem?.Name ?? "F&B"} x{f.Quantity}"))
                    : "—";

                return new
                {
                    id = b.Id,
                    bookingCode = b.BookingCode,
                    bookingDate = b.BookingDate.ToString("dd/MM/yyyy HH:mm"),
                    customerName = b.CustomerName,
                    customerPhone = b.CustomerPhone,
                    customerEmail = b.CustomerEmail,
                    movieTitle = b.Showtime?.Movie?.Title ?? "—",
                    cinemaName = b.Showtime?.CinemaHall?.Cinema?.Name ?? "—",
                    hallName = b.Showtime?.CinemaHall?.Name ?? "—",
                    seatsList = b.SeatsList,
                    ticketsCount = seatCount,
                    ticketAmount = ticket,
                    foodAmount = food,
                    totalAmount = b.TotalAmount,
                    paymentMethod = b.PaymentMethod,
                    status = b.Status,
                    foodDesc = foodDesc
                };
            }).ToList();

            return Json(new
            {
                hasData = true,
                message = "Tải báo cáo thành công.",
                fromDate = start.ToString("yyyy-MM-dd"),
                toDate = (toDate?.Date ?? today).ToString("yyyy-MM-dd"),
                summary = new
                {
                    totalRevenue,
                    totalTicketRevenue,
                    totalFoodRevenue,
                    totalTicketsSold,
                    totalOrders,
                    occupancyRate,
                    avgOrderValue
                },
                timeline,
                byMovie,
                byCinema,
                byPayment,
                details
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenueReport(DateTime? fromDate, DateTime? toDate, int? cinemaId, int? movieId)
        {
            var today = DateTime.Today;
            var start = fromDate?.Date ?? today.AddDays(-29);
            var end = (toDate?.Date ?? today).AddDays(1).AddTicks(-1);

            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .Include(b => b.BookingFoodItems)
                    .ThenInclude(bf => bf.FoodItem)
                .Where(b => b.Status == "Đã thanh toán" && !b.IsDeleted && b.BookingDate >= start && b.BookingDate <= end);

            if (cinemaId.HasValue && cinemaId.Value > 0)
            {
                query = query.Where(b => b.Showtime != null && b.Showtime.CinemaHall != null && b.Showtime.CinemaHall.CinemaId == cinemaId.Value);
            }
            if (movieId.HasValue && movieId.Value > 0)
            {
                query = query.Where(b => b.Showtime != null && b.Showtime.MovieId == movieId.Value);
            }

            var bookings = await query.OrderByDescending(b => b.BookingDate).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.Append('\uFEFF'); // UTF-8 BOM để Excel hiển thị đúng tiếng Việt

            sb.AppendLine("BÁO CÁO DOANH THU & BÁN VÉ CGV CINEMAS");
            sb.AppendLine($"Khoảng thời gian:;{start:dd/MM/yyyy} - {(toDate?.Date ?? today):dd/MM/yyyy}");
            sb.AppendLine($"Thời điểm xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Người xuất báo cáo:;{User.Identity?.Name ?? "Quản trị viên"}");
            sb.AppendLine();

            decimal totalRev = bookings.Sum(b => b.TotalAmount);
            decimal totalFood = bookings.Sum(b => b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0);
            decimal totalTicket = totalRev - totalFood;
            int totalTix = bookings.Sum(b => !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1);

            sb.AppendLine("TỔNG HỢP CHỈ SỐ DOANH THU:");
            sb.AppendLine($"Tổng Doanh Thu (VNĐ):;{totalRev:N0}");
            sb.AppendLine($"Doanh Thu Bán Vé (VNĐ):;{totalTicket:N0}");
            sb.AppendLine($"Doanh Thu Bắp Nước F&B (VNĐ):;{totalFood:N0}");
            sb.AppendLine($"Tổng Số Vé Bán Ra:;{totalTix}");
            sb.AppendLine($"Tổng Số Đơn Đặt Vé:;{bookings.Count}");
            sb.AppendLine();

            sb.AppendLine("DANH SÁCH CHI TIẾT CÁC ĐƠN ĐẶT VÉ ĐÃ THANH TOÁN:");
            sb.AppendLine("STT;Mã Đơn;Thời Gian;Khách Hàng;Số Điện Thoại;Phim;Cụm Rạp;Phòng Chiếu;Danh Sách Ghế;Số Vé;Tiền Vé (VNĐ);Tiền Bắp Nước (VNĐ);Tổng Tiền (VNĐ);Hình Thức TT;Trạng Thái");

            int idx = 1;
            foreach (var b in bookings)
            {
                var food = b.BookingFoodItems?.Sum(f => (decimal)f.Quantity * f.UnitPrice) ?? 0;
                var ticket = b.TotalAmount > food ? b.TotalAmount - food : b.TotalAmount;
                var tix = !string.IsNullOrWhiteSpace(b.SeatsList) ? b.SeatsList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 1;
                var movie = (b.Showtime?.Movie?.Title ?? "").Replace(";", ",");
                var cinema = (b.Showtime?.CinemaHall?.Cinema?.Name ?? "").Replace(";", ",");
                var hall = (b.Showtime?.CinemaHall?.Name ?? "").Replace(";", ",");
                var cust = (b.CustomerName ?? "").Replace(";", ",");
                var seats = (b.SeatsList ?? "").Replace(";", ",");

                sb.AppendLine($"{idx};{b.BookingCode};{b.BookingDate:dd/MM/yyyy HH:mm};{cust};'{b.CustomerPhone};{movie};{cinema};{hall};{seats};{tix};{ticket:0};{food:0};{b.TotalAmount:0};{b.PaymentMethod};{b.Status}");
                idx++;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"BaoCaoDoanhThu_CGV_{start:yyyyMMdd}_{(toDate?.Date ?? today):yyyyMMdd}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        #endregion
    }
}

