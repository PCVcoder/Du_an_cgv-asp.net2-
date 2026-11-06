using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
            if (startTime == default || startTime.Year < 2000)
            {
                return Json(new { hasConflict = true, message = "⚠️ Thời gian bắt đầu chiếu không hợp lệ. Vui lòng chọn đầy đủ ngày và giờ!" });
            }

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return Json(new { hasConflict = false, message = "Vui lòng chọn phim" });
            }

            if (startTime < DateTime.Now.AddMinutes(-10))
            {
                return Json(new
                {
                    hasConflict = true,
                    message = $"⚠️ THỜI GIAN TRONG QUÁ KHỨ: Thời gian bắt đầu ({startTime:dd/MM/yyyy HH:mm}) đã qua so với hiện tại ({DateTime.Now:dd/MM/yyyy HH:mm}). Vui lòng chọn khung giờ từ hiện tại trở đi!"
                });
            }

            if (movie.EndDate.HasValue && startTime.Date > movie.EndDate.Value.Date)
            {
                return Json(new
                {
                    hasConflict = true,
                    message = $"⚠️ VƯỢT QUÁ HẠN CHIẾU: Phim \"{movie.Title}\" chỉ chiếu đến ngày {movie.EndDate.Value:dd/MM/yyyy}!"
                });
            }

            if (startTime.Date < movie.ReleaseDate.Date)
            {
                return Json(new
                {
                    hasConflict = true,
                    message = $"⚠️ CHƯA KHỞI CHIẾU: Phim \"{movie.Title}\" khởi chiếu từ ngày {movie.ReleaseDate:dd/MM/yyyy}!"
                });
            }

            if (startTime.Hour >= 1 && startTime.Hour < 7)
            {
                return Json(new
                {
                    hasConflict = true,
                    message = $"⚠️ NGOÀI GIỜ HOẠT ĐỘNG: Khung giờ {startTime:HH:mm} thuộc thời gian rạp nghỉ đêm (01:00 - 07:00). Vui lòng chọn từ 08:00 đến 24:00!"
                });
            }

            var duration = movie.Duration > 0 ? movie.Duration : 120;
            var endTime = startTime.AddMinutes(duration);

            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == cinemaHallId && (excludeShowtimeId == 0 || s.Id != excludeShowtimeId) && !s.IsDeleted)
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
                    duration = duration,
                    endTime = endTime.ToString("HH:mm (dd/MM/yyyy)"),
                    message = $"⚠️ TRÙNG LỊCH CHIẾU: {hallName} ({cinemaName}) đã có lịch chiếu phim \"{movieTitle}\" từ {startStr} đến {endStr}!"
                });
            }

            return Json(new
            {
                hasConflict = false,
                duration = duration,
                endTime = endTime.ToString("HH:mm (dd/MM/yyyy)"),
                message = $"✅ Khung giờ hợp lệ! Kết thúc dự kiến: {endTime:HH:mm (dd/MM/yyyy)} (Thời lượng: {duration} phút)"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShowtime(int MovieId, int CinemaHallId, DateTime StartTime, decimal Price, string? Format)
        {
            if (StartTime == default || StartTime.Year < 2000)
            {
                TempData["ErrorMessage"] = "⚠️ Thời gian bắt đầu chiếu không hợp lệ. Vui lòng kiểm tra lại ngày và giờ!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime < DateTime.Now.AddMinutes(-10))
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Thời gian chiếu ({StartTime:dd/MM/yyyy HH:mm}) đã qua so với thời gian hiện tại ({DateTime.Now:dd/MM/yyyy HH:mm}). Không thể lên lịch suất chiếu ở quá khứ!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var movie = await _context.Movies.FindAsync(MovieId);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "⚠️ Không tìm thấy phim được chọn.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (movie.EndDate.HasValue && StartTime.Date > movie.EndDate.Value.Date)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Phim \"{movie.Title}\" chỉ chiếu đến hết ngày {movie.EndDate.Value:dd/MM/yyyy}. Thời gian bạn chọn ({StartTime:dd/MM/yyyy}) đã vượt quá thời hạn chiếu của phim!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime.Date < movie.ReleaseDate.Date)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Phim \"{movie.Title}\" khởi chiếu từ ngày {movie.ReleaseDate:dd/MM/yyyy}. Không thể tạo suất chiếu trước ngày khởi chiếu!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime.Hour >= 1 && StartTime.Hour < 7)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Giờ bắt đầu ({StartTime:HH:mm}) nằm trong khung giờ rạp đóng cửa nghỉ đêm (01:00 - 07:00)! Vui lòng chọn khung giờ từ 08:00 đến 24:00.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var duration = movie.Duration > 0 ? movie.Duration : 120;
            var endTime = StartTime.AddMinutes(duration);

            // Kiểm tra trùng lịch chiếu trong cùng phòng chiếu
            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == CinemaHallId && !s.IsDeleted)
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
                Price = Price > 0 ? Price : 105000,
                Format = string.IsNullOrWhiteSpace(Format) ? "2D" : Format.Trim()
            };

            try
            {
                _context.Showtimes.Add(showtime);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"⚠️ Không thể lưu suất chiếu mới: {ex.Message}";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var hallInfo = await _context.CinemaHalls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == CinemaHallId);
            await LogActionAsync("Thêm mới", "Suất chiếu", $"Tạo suất chiếu {showtime.Format} phim \"{movie.Title}\" ({StartTime:dd/MM/yyyy HH:mm}) tại {hallInfo?.Cinema?.Name} - {hallInfo?.Name}", showtime.Id.ToString(), hallInfo?.CinemaId);

            TempData["SuccessMessage"] = "Đã thêm suất chiếu mới thành công!";
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShowtime(int Id, int MovieId, int CinemaHallId, DateTime StartTime, decimal Price, string? Format)
        {
            var existing = await _context.Showtimes.FindAsync(Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "⚠️ Không tìm thấy suất chiếu cần sửa.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime == default || StartTime.Year < 2000)
            {
                TempData["ErrorMessage"] = "⚠️ Thời gian bắt đầu chiếu không hợp lệ. Vui lòng kiểm tra lại ngày và giờ!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime < DateTime.Now.AddMinutes(-10))
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Thời gian chiếu ({StartTime:dd/MM/yyyy HH:mm}) đã qua so với thời gian hiện tại ({DateTime.Now:dd/MM/yyyy HH:mm}). Không thể lên lịch suất chiếu ở quá khứ!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var movie = await _context.Movies.FindAsync(MovieId);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "⚠️ Không tìm thấy phim được chọn cho suất chiếu này.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (movie.EndDate.HasValue && StartTime.Date > movie.EndDate.Value.Date)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Phim \"{movie.Title}\" chỉ chiếu đến hết ngày {movie.EndDate.Value:dd/MM/yyyy}. Thời gian bạn chọn ({StartTime:dd/MM/yyyy}) đã vượt quá thời hạn chiếu của phim!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime.Date < movie.ReleaseDate.Date)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Phim \"{movie.Title}\" khởi chiếu từ ngày {movie.ReleaseDate:dd/MM/yyyy}. Không thể tạo suất chiếu trước ngày khởi chiếu!";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            if (StartTime.Hour >= 1 && StartTime.Hour < 7)
            {
                TempData["ErrorMessage"] = $"⚠️ THỜI GIAN KHÔNG HỢP LỆ: Giờ bắt đầu ({StartTime:HH:mm}) nằm trong khung giờ rạp đóng cửa nghỉ đêm (01:00 - 07:00)! Vui lòng chọn khung giờ từ 08:00 đến 24:00.";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

            var duration = movie.Duration > 0 ? movie.Duration : 120;
            var endTime = StartTime.AddMinutes(duration);

            // Kiểm tra trùng lịch chiếu trong cùng phòng chiếu (loại trừ chính suất đang sửa và suất đã ẩn)
            var conflicting = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaHall)
                    .ThenInclude(h => h!.Cinema)
                .Where(s => s.CinemaHallId == CinemaHallId && s.Id != Id && !s.IsDeleted)
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

            // Đảm bảo Format không bao giờ bị null
            string finalFormat = !string.IsNullOrWhiteSpace(Format) 
                ? Format.Trim() 
                : (!string.IsNullOrWhiteSpace(existing.Format) ? existing.Format : "2D");

            existing.MovieId = MovieId;
            existing.CinemaHallId = CinemaHallId;
            existing.StartTime = StartTime;
            existing.EndTime = endTime;
            existing.Price = Price > 0 ? Price : (existing.Price > 0 ? existing.Price : 105000);
            existing.Format = finalFormat;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"⚠️ Không thể cập nhật suất chiếu: {ex.Message}";
                return RedirectToAction("Index", new { tab = "showtimes" });
            }

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

                showtime.IsDeleted = true;
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Suất chiếu", $"Ẩn suất chiếu #{id} ({showtime.StartTime:dd/MM/yyyy HH:mm}) tại {cinemaName} - {hallName}", id.ToString(), cinemaId);
                TempData["SuccessMessage"] = "Đã ẩn suất chiếu! Suất chiếu không xuất hiện với khách hàng nhưng lịch sử vẫn được bảo toàn.";
            }
            return RedirectToAction("Index", new { tab = "showtimes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreShowtime(int id)
        {
            var showtime = await _context.Showtimes.FindAsync(id);
            if (showtime != null)
            {
                showtime.IsDeleted = false;
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Suất chiếu", $"Khôi phục suất chiếu #{id}", id.ToString());
                TempData["SuccessMessage"] = "Đã khôi phục hiển thị suất chiếu!";
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
    }
}

