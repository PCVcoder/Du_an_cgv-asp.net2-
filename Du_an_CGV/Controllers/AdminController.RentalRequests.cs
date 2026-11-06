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
        public async Task<IActionResult> UpdateRentalDetails(
            int id, 
            string status, 
            string? adminNotes, 
            decimal? estimatedBudget,
            string? timeSlot,
            string? movieTitle,
            string? bookingMode,
            string? allocatedHalls,
            bool isHoldLocked,
            int? holdHours)
        {
            var rental = await _context.HallRentalRequests.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);
            if (rental != null)
            {
                rental.Status = status;
                rental.AdminNotes = adminNotes;
                rental.EstimatedBudget = estimatedBudget;
                if (!string.IsNullOrWhiteSpace(timeSlot)) rental.TimeSlot = timeSlot;
                if (!string.IsNullOrWhiteSpace(movieTitle)) rental.MovieTitle = movieTitle;
                if (!string.IsNullOrWhiteSpace(bookingMode)) rental.BookingMode = bookingMode;
                if (!string.IsNullOrWhiteSpace(allocatedHalls)) rental.AllocatedHalls = allocatedHalls;
                rental.IsHoldLocked = isHoldLocked;
                if (isHoldLocked && (!rental.HoldExpiresAt.HasValue || rental.HoldExpiresAt.Value < DateTime.Now))
                {
                    rental.HoldExpiresAt = DateTime.Now.AddHours(holdHours ?? 48);
                }
                else if (!isHoldLocked)
                {
                    rental.HoldExpiresAt = null;
                }

                await _context.SaveChangesAsync();
                var budgetStr = estimatedBudget.HasValue ? $"{estimatedBudget.Value:N0}đ" : "Chưa nhập";
                await LogActionAsync("Xử lý chi tiết", "Thuê rạp & Sự kiện", $"Cập nhật đơn {rental.FullName}: Trạng thái \"{status}\", Khung giờ \"{rental.TimeSlot}\", Phim \"{rental.MovieTitle}\", Khóa giữ chỗ: {(rental.IsHoldLocked ? "Đang khóa" : "Không")}", id.ToString(), rental.CinemaId);
                TempData["SuccessMessage"] = $"Đã lưu tiến độ xử lý và cấu hình thời gian/suất chiếu cho khách {rental.FullName}!";
            }
            return RedirectToAction("Index", new { tab = "rentals" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRentalHoldLock(int id, bool lockStatus, int holdHours = 48)
        {
            var rental = await _context.HallRentalRequests.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.Id == id);
            if (rental != null)
            {
                rental.IsHoldLocked = lockStatus;
                if (lockStatus)
                {
                    rental.HoldExpiresAt = DateTime.Now.AddHours(holdHours);
                    if (rental.Status == "Mới tiếp nhận") rental.Status = "Đang tư vấn / Khảo sát rạp";
                    await LogActionAsync("Khóa giữ chỗ", "Thuê rạp & Sự kiện", $"Khóa giữ phòng/suất tạm thời cho đơn {rental.FullName} trong {holdHours}h để đợi cọc.", id.ToString(), rental.CinemaId);
                    TempData["SuccessMessage"] = $"🔒 Đã kích hoạt KHÓA TẠM THỜI {holdHours}h cho đơn của {rental.FullName}! Phòng và khung giờ này đã được bảo vệ tránh bán vé lẻ.";
                }
                else
                {
                    rental.HoldExpiresAt = null;
                    await LogActionAsync("Mở khóa giữ chỗ", "Thuê rạp & Sự kiện", $"Mở khóa giữ phòng/suất cho đơn {rental.FullName}.", id.ToString(), rental.CinemaId);
                    TempData["SuccessMessage"] = $"🔓 Đã MỞ KHÓA giữ chỗ cho đơn {rental.FullName}! Các phòng và suất chiếu có thể mở bán vé lẻ lại.";
                }
                await _context.SaveChangesAsync();
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

