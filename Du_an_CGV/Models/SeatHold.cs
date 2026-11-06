using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Du_an_CGV.Models
{
    [Table("GIUCHO")]
    public class SeatHold
    {
        [Key]
        [Column("MaGiuCho")]
        [Display(Name = "Mã giữ chỗ")]
        public int Id { get; set; }

        [Required]
        [Column("MaSuatChieu")]
        [Display(Name = "Mã suất chiếu")]
        public int ShowtimeId { get; set; }
        public Showtime? Showtime { get; set; }

        [Required]
        [Column("MaGhe")]
        [StringLength(20)]
        [Display(Name = "Ghế giữ chỗ")]
        public string SeatCode { get; set; } = string.Empty; // e.g. "C4"

        [Column("MaKH")]
        [Display(Name = "Mã khách hàng")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        [Column("ThoiGianHetHan")]
        [Display(Name = "Thời gian hết hạn")]
        public DateTime ExpiresAt { get; set; }

        [Display(Name = "Thời gian tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsExpired => DateTime.Now >= ExpiresAt;

        [NotMapped]
        public int RemainingSeconds => Math.Max(0, (int)(ExpiresAt - DateTime.Now).TotalSeconds);
    }
}

