using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
                cinema.IsDeleted = true;
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Cụm rạp", $"Ẩn cụm rạp \"{name}\" ({city})", id.ToString());
                TempData["SuccessMessage"] = $"Đã ẩn rạp \"{name}\"! Toàn bộ dữ liệu được bảo toàn và có thể khôi phục lại.";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreCinema(int id)
        {
            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema != null)
            {
                cinema.IsDeleted = false;
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Cụm rạp", $"Khôi phục cụm rạp \"{cinema.Name}\"", id.ToString());
                TempData["SuccessMessage"] = $"Đã khôi phục rạp \"{cinema.Name}\"!";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        #endregion
    }
}

