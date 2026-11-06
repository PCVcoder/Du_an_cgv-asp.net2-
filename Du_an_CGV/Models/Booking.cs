using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã đặt vé")]
        public string BookingCode { get; set; } = string.Empty;

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Suất chiếu")]
        public int ShowtimeId { get; set; }
        public Showtime? Showtime { get; set; }

        [Display(Name = "Thời gian đặt vé")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Vui lòng chọn ghế ngồi")]
        [Display(Name = "Danh sách ghế")]
        public string SeatsList { get; set; } = string.Empty; // e.g. "C4, C5"

        [Required]
        [Display(Name = "Tổng tiền (VNĐ)")]
        public decimal TotalAmount { get; set; } = 0;

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Đã thanh toán"; // Đã thanh toán, Chờ xử lý, Đã hủy

        [Display(Name = "Hình thức thanh toán")]
        public string PaymentMethod { get; set; } = "VNPAY-QR"; // VNPAY-QR, ZaloPay, MoMo, Thẻ ATM/Visa

        [Display(Name = "Đã ẩn")]
        public bool IsDeleted { get; set; } = false;

        // Danh sách chi tiết bắp nước đã đặt kèm vé
        public ICollection<BookingFoodItem> BookingFoodItems { get; set; } = new List<BookingFoodItem>();
    }
}

