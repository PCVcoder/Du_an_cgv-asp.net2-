using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
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
                inquiry.IsDeleted = true;
                inquiry.Status = "Đã ẩn";
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Tiếp nhận TT", $"Ẩn thư liên hệ của {name} (ID: {id})", id.ToString());
                TempData["SuccessMessage"] = "Đã ẩn thư liên hệ (không xóa vĩnh viễn)!";
            }
            return RedirectToAction("Index", new { tab = "inquiries" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreInquiry(int id)
        {
            var inquiry = await _context.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.IsDeleted = false;
                inquiry.Status = "Chưa đọc";
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Tiếp nhận TT", $"Khôi phục thư liên hệ của {inquiry.FullName} (ID: {id})", id.ToString());
                TempData["SuccessMessage"] = "Đã khôi phục hiển thị thư liên hệ!";
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
        public async Task<IActionResult> UpdateRentalDetails(int id, string status, string? adminNotes, decimal? estimatedBudget)
        {
            var rental = await _context.HallRentalRequests.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);
            if (rental != null)
            {
                rental.Status = status;
                rental.AdminNotes = adminNotes;
                rental.EstimatedBudget = estimatedBudget;
                await _context.SaveChangesAsync();
                var budgetStr = estimatedBudget.HasValue ? $"{estimatedBudget.Value:N0}đ" : "Chưa nhập";
                await LogActionAsync("Xử lý chi tiết", "Thuê rạp & Sự kiện", $"Cập nhật tiến độ & báo giá đơn của {rental.FullName}: Trạng thái \"{status}\", Dự toán: {budgetStr}", id.ToString(), rental.CinemaId);
                TempData["SuccessMessage"] = $"Đã lưu tiến độ xử lý và dự toán chi phí cho khách {rental.FullName}!";
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
                rental.IsDeleted = true;
                rental.Status = "Đã hủy (Ẩn)";
                await _context.SaveChangesAsync();
                await LogActionAsync("Ẩn", "Thuê rạp & Sự kiện", $"Ẩn yêu cầu thuê rạp của {name} (ID: {id})", id.ToString(), cinemaId);
                TempData["SuccessMessage"] = "Đã ẩn yêu cầu thuê rạp (không xóa vĩnh viễn)!";
            }
            return RedirectToAction("Index", new { tab = "rentals" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreRental(int id)
        {
            var rental = await _context.HallRentalRequests.FindAsync(id);
            if (rental != null)
            {
                rental.IsDeleted = false;
                rental.Status = "Mới tiếp nhận";
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Thuê rạp & Sự kiện", $"Khôi phục yêu cầu thuê rạp của {rental.FullName} (ID: {id})", id.ToString(), rental.CinemaId);
                TempData["SuccessMessage"] = "Đã khôi phục hiển thị yêu cầu thuê rạp!";
            }
            return RedirectToAction("Index", new { tab = "rentals" });
        }

        #endregion
    }
}

