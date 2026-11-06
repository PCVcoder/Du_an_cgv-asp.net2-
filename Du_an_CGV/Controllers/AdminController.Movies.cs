using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
                movie.IsDeleted = true;
                movie.IsNowShowing = false;
                movie.IsUpcoming = false;
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Phim", $"Chuyển phim \"{title}\" (ID: {id}) vào danh sách ẩn", id.ToString());
                TempData["SuccessMessage"] = $"Đã ẩn phim \"{title}\"! Dữ liệu vẫn được bảo toàn và có thể khôi phục lại bất kỳ lúc nào.";
            }
            return RedirectToAction("Index", new { tab = "movies" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                movie.IsDeleted = false;
                movie.IsNowShowing = true;
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Phim", $"Khôi phục phim \"{movie.Title}\" (ID: {id})", id.ToString());
                TempData["SuccessMessage"] = $"Đã khôi phục hiển thị phim \"{movie.Title}\"!";
            }
            return RedirectToAction("Index", new { tab = "movies" });
        }

        #endregion
    }
}

