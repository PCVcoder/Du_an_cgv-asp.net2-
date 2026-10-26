using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class Showtime
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phim")]
        [Display(Name = "Phim")]
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu")]
        [Display(Name = "Phòng chiếu")]
        public int CinemaHallId { get; set; }
        public CinemaHall? CinemaHall { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime StartTime { get; set; }

        [Display(Name = "Thời gian kết thúc")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá vé")]
        [Display(Name = "Giá vé cơ bản (VNĐ)")]
        public decimal Price { get; set; } = 110000;

        [Display(Name = "Định dạng")]
        public string Format { get; set; } = "2D"; // 2D, 3D, IMAX, 4DX, L'Amour

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
