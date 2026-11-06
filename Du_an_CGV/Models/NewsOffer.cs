using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class NewsOffer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề ưu đãi / tin tức")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phân loại")]
        [Display(Name = "Mã phân loại")]
        public string Category { get; set; } = "offers"; // offers, news, member, event

        [Required(ErrorMessage = "Vui lòng nhập nhãn hiển thị")]
        [Display(Name = "Nhãn danh mục")]
        public string CategoryLabel { get; set; } = "ƯU ĐÃI"; // ƯU ĐÃI, TIN MỚI, THÀNH VIÊN, SỰ KIỆN

        [Display(Name = "Thời gian áp dụng")]
        public string DateRange { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập link hình ảnh")]
        [Display(Name = "Hình ảnh banner")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tóm tắt nội dung")]
        [Display(Name = "Tóm tắt")]
        public string Summary { get; set; } = string.Empty;

        [Display(Name = "Chi tiết nội dung")]
        public string? Content { get; set; }

        [Display(Name = "Đang chạy / Hiển thị")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Đã ẩn")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Phim liên quan (nếu có)")]
        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }

        [Display(Name = "Cụm rạp áp dụng (nếu có)")]
        public int? CinemaId { get; set; }
        public Cinema? Cinema { get; set; }

        [Display(Name = "Người đăng bài")]
        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}

