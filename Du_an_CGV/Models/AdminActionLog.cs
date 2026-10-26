using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class AdminActionLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Loại thao tác")]
        public string ActionType { get; set; } = "Cập nhật"; // Thêm mới, Cập nhật, Xóa, Đổi trạng thái, Giao dịch vé, Đăng nhập

        [Required]
        [Display(Name = "Phân hệ")]
        public string ModuleName { get; set; } = "Hệ thống"; // Đơn vé & Giao dịch, Suất chiếu, Cụm rạp, Phim, Bắp nước & F&B, Tin tức & Ưu đãi, Tiếp nhận TT, Thuê rạp, Hệ thống

        [Required]
        [Display(Name = "Chi tiết thao tác")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Mã tham chiếu")]
        public string? EntityId { get; set; }

        [Required]
        [Display(Name = "Người thực hiện")]
        public string PerformedBy { get; set; } = "Quản trị viên CGV";

        [Display(Name = "Địa chỉ IP")]
        public string? IpAddress { get; set; } = "127.0.0.1";

        [Display(Name = "Cụm rạp")]
        public int? CinemaId { get; set; }
        public Cinema? Cinema { get; set; }

        [Display(Name = "Tỉnh / Thành phố")]
        public string? City { get; set; }

        [Display(Name = "Thời gian thực hiện")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Thành công"; // Thành công, Cảnh báo, Thất bại
    }
}
