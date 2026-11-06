using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
        #region ===================== QUẢN LÝ ĐẶT VÉ (BOOKINGS) =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status, string? returnTab = "bookings")
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                var oldStatus = booking.Status;
                booking.Status = status;
                await _context.SaveChangesAsync();

                int? cinemaId = booking.Showtime?.CinemaHall?.CinemaId;
                await LogActionAsync("Đổi trạng thái", "Đơn vé & Giao dịch",
                    $"Cập nhật trạng thái đơn vé '{booking.BookingCode}' từ '{oldStatus}' thành '{status}' (Khách: {booking.CustomerName}, Tổng tiền: {booking.TotalAmount:N0}đ)",
                    booking.BookingCode, cinemaId);

                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn #{booking.BookingCode} thành \"{status}\"!";
            }
            return RedirectToAction("Index", new { tab = string.IsNullOrWhiteSpace(returnTab) ? "bookings" : returnTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(int id, string? returnTab = "bookings")
        {
            var booking = await _context.Bookings
                .Include(b => b.Showtime)
                    .ThenInclude(s => s!.CinemaHall)
                        .ThenInclude(h => h!.Cinema)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                var code = booking.BookingCode;
                int? cinemaId = booking.Showtime?.CinemaHall?.CinemaId;
                booking.IsDeleted = true;
                booking.Status = "Đã hủy (Ẩn)";
                await _context.SaveChangesAsync();

                await LogActionAsync("Ẩn", "Đơn vé & Giao dịch", $"Ẩn đơn đặt vé #{code} (Khách: {booking.CustomerName}, Tiền: {booking.TotalAmount:N0}đ)", code, cinemaId);
                TempData["SuccessMessage"] = $"Đã chuyển đơn đặt vé #{code} sang trạng thái Đã hủy (Ẩn)! Lịch sử thanh toán được bảo toàn.";
            }
            return RedirectToAction("Index", new { tab = string.IsNullOrWhiteSpace(returnTab) ? "bookings" : returnTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreBooking(int id, string? returnTab = "bookings")
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.IsDeleted = false;
                booking.Status = "Đã thanh toán";
                await _context.SaveChangesAsync();
                await LogActionAsync("Khôi phục", "Đơn vé & Giao dịch", $"Khôi phục đơn vé #{booking.BookingCode}", booking.BookingCode);
                TempData["SuccessMessage"] = $"Đã khôi phục hiển thị đơn đặt vé #{booking.BookingCode}!";
            }
            return RedirectToAction("Index", new { tab = string.IsNullOrWhiteSpace(returnTab) ? "bookings" : returnTab });
        }

        #endregion
    }
}

