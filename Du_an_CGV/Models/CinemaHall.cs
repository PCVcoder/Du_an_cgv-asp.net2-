using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class CinemaHall
    {
        public int Id { get; set; }

        public int CinemaId { get; set; }
        public Cinema? Cinema { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng chiếu")]
        [Display(Name = "Tên phòng chiếu")]
        public string Name { get; set; } = "Cinema 01";

        [Display(Name = "Loại phòng chiếu")]
        public string HallType { get; set; } = "Standard 2D"; // Standard 2D, IMAX, 4DX, Gold Class, L'Amour

        [Display(Name = "Tổng số ghế")]
        public int TotalSeats { get; set; } = 96;

        [Display(Name = "Đã ẩn")]
        public bool IsDeleted { get; set; } = false;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}
