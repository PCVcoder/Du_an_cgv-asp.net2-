using System.ComponentModel.DataAnnotations;

namespace Du_an_CGV.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phim")]
        [Display(Name = "Tên phim")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thể loại")]
        [Display(Name = "Thể loại")]
        public string Genre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thời lượng")]
        [Display(Name = "Thời lượng (phút)")]
        public int Duration { get; set; } = 120;

        [Required(ErrorMessage = "Vui lòng chọn phân loại độ tuổi")]
        [Display(Name = "Phân loại độ tuổi")]
        public string Rating { get; set; } = "T16";

        [Display(Name = "Mã màu phân loại")]
        public string RatingColor { get; set; } = "#2e7d32";

        [Required(ErrorMessage = "Vui lòng nhập ngày khởi chiếu")]
        [Display(Name = "Ngày khởi chiếu")]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; } = DateTime.Today;

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập link Poster")]
        [Display(Name = "Link Poster")]
        public string PosterUrl { get; set; } = string.Empty;

        [Display(Name = "Link Banner")]
        public string? BannerUrl { get; set; }

        [Display(Name = "Link Trailer (YouTube)")]
        public string? TrailerUrl { get; set; }

        [Display(Name = "Đạo diễn")]
        public string? Director { get; set; }

        [Display(Name = "Diễn viên")]
        public string? Cast { get; set; }

        [Display(Name = "Ngôn ngữ")]
        public string? Language { get; set; } = "Tiếng Việt - Phụ đề Tiếng Anh";

        [Display(Name = "Định dạng chiếu")]
        public string? Formats { get; set; } = "2D DIGITAL, 3D, IMAX";

        [Display(Name = "Tóm tắt nội dung")]
        public string? Synopsis { get; set; }

        [Display(Name = "Đang chiếu")]
        public bool IsNowShowing { get; set; } = true;

        [Display(Name = "Sắp chiếu")]
        public bool IsUpcoming { get; set; } = false;

        [Display(Name = "Thứ hạng nổi bật")]
        public int? Rank { get; set; }

        [Display(Name = "Nhãn đặc biệt")]
        public string? SpecialTag { get; set; }

        [Display(Name = "Đã ẩn")]
        public bool IsDeleted { get; set; } = false;

        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
        public ICollection<NewsOffer> NewsOffers { get; set; } = new List<NewsOffer>();
    }
}

