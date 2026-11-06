using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
                item.IsDeleted = true;
                item.IsAvailable = false;
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Bắp nước & F&B", $"Ẩn món \"{name}\" khỏi menu F&B", id.ToString());
                TempData["SuccessMessage"] = $"Đã ẩn món \"{name}\" khỏi menu! Dữ liệu được bảo toàn.";
            }
            return RedirectToAction("Index", new { tab = "food" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreFoodItem(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.IsAvailable = true;
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Bắp nước & F&B", $"Khôi phục món \"{item.Name}\" vào menu", id.ToString());
                TempData["SuccessMessage"] = $"Đã khôi phục món \"{item.Name}\" vào menu bán hàng!";
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

