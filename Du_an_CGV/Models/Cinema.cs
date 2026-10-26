using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class Cinema
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên rạp")]
        [Display(Name = "Tên rạp CGV")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh / Thành phố")]
        [Display(Name = "Tỉnh / Thành phố")]
        public string City { get; set; } = "Hồ Chí Minh";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Hotline liên hệ")]
        public string Hotline { get; set; } = "1900 6017";

        [Display(Name = "Hình ảnh rạp")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Giới thiệu rạp")]
        public string? Description { get; set; }

        public ICollection<CinemaHall> CinemaHalls { get; set; } = new List<CinemaHall>();
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
        public ICollection<HallRentalRequest> HallRentalRequests { get; set; } = new List<HallRentalRequest>();
        public ICollection<NewsOffer> NewsOffers { get; set; } = new List<NewsOffer>();
    }
}

