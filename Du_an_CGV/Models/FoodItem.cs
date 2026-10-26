using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên combo/món")]
        [Display(Name = "Tên Combo / Món F&B")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phân loại")]
        [Display(Name = "Phân loại")]
        public string Category { get; set; } = "Combo"; // Combo, Bắp rang, Nước uống, Snack

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Display(Name = "Đơn giá (VNĐ)")]
        [Range(0, 10000000, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh món")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [Display(Name = "Đang kinh doanh")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; } = 0;

        // Danh sách các đơn đặt vé có mua món ăn này
        public ICollection<BookingFoodItem> BookingFoodItems { get; set; } = new List<BookingFoodItem>();
    }
}

