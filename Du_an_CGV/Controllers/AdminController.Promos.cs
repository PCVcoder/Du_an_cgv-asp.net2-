using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
                promo.IsDeleted = true;
                promo.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã ẩn ưu đãi / tin tức (không xóa vĩnh viễn)!";
            }
            return RedirectToAction("Index", new { tab = "promos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePromo(int id)
        {
            var promo = await _context.NewsOffers.FindAsync(id);
            if (promo != null)
            {
                promo.IsDeleted = false;
                promo.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục hiển thị ưu đãi / tin tức!";
            }
            return RedirectToAction("Index", new { tab = "promos" });
        }

        #endregion
    }
}

