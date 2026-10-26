using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class BookingFoodItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Mã đơn vé")]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        [Display(Name = "Món bắp nước")]
        public int FoodItemId { get; set; }
        public FoodItem? FoodItem { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }
    }
}
