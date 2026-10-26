using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class ContactInquiry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề liên hệ")]
        [Display(Name = "Chủ đề")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung liên hệ")]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Ngày gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chưa đọc"; // Chưa đọc, Đã đọc, Đã phản hồi

        [Display(Name = "Ghi chú của ban quản trị")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Tài khoản người gửi (nếu có)")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}

