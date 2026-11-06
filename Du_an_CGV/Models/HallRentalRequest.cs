using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class HallRentalRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Tên công ty / tổ chức")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn rạp mong muốn")]
        [Display(Name = "Cụm rạp CGV")]
        public string CinemaName { get; set; } = string.Empty;

        [Display(Name = "Rạp chiếu")]
        public int? CinemaId { get; set; }
        public Cinema? Cinema { get; set; }

        [Display(Name = "Tài khoản người dùng (nếu có)")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Display(Name = "Ngày dự kiến tổ chức")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        [Display(Name = "Số lượng khách dự kiến")]
        public int GuestCount { get; set; } = 50;

        [Display(Name = "Loại dịch vụ")]
        public string ServiceType { get; set; } = "Thuê trọn rạp tổ chức sự kiện"; // Thuê trọn rạp tổ chức sự kiện, Đặt vé xem phim theo nhóm, Gói L'Amour đặc biệt

        [Display(Name = "Yêu cầu chi tiết")]
        public string? Notes { get; set; }

        [Display(Name = "Ngày gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Mới tiếp nhận"; // Mới tiếp nhận, Đang tư vấn, Chờ khảo sát, Đã chốt, Đã hủy (Ẩn)

        [Display(Name = "Ghi chú QTV / Tiến độ xử lý")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Dự toán / Báo giá sơ bộ (VNĐ)")]
        public decimal? EstimatedBudget { get; set; }

        [Display(Name = "Đã ẩn")]
        public bool IsDeleted { get; set; } = false;
    }
}

