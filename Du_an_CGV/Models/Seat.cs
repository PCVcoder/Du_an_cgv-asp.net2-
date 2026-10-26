using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class Seat
    {
        public int Id { get; set; }

        public int CinemaHallId { get; set; }
        public CinemaHall? CinemaHall { get; set; }

        [Required]
        [Display(Name = "Hàng ghế")]
        public string Row { get; set; } = "A";

        [Required]
        [Display(Name = "Số ghế")]
        public int Number { get; set; } = 1;

        [Required]
        [Display(Name = "Mã ghế")]
        public string SeatCode { get; set; } = "A1";

        [Display(Name = "Loại ghế")]
        public string SeatType { get; set; } = "Standard"; // Standard, VIP, Sweetbox

        [Display(Name = "Phụ thu loại ghế")]
        public decimal PriceModifier { get; set; } = 0;
    }
}
