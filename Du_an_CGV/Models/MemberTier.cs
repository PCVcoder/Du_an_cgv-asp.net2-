using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Du_an_CGV.Models
{
    [Table("CAPBAC_THANHVIEN")]
    public class MemberTier
    {
        [Key]
        [Column("MaCapBac")]
        [Display(Name = "Mã cấp bậc")]
        public int Id { get; set; }

        [Required]
        [Column("TenCapBac")]
        [StringLength(100)]
        [Display(Name = "Tên cấp bậc")]
        public string TierName { get; set; } = string.Empty;

        [Column("DiemYeuCau")]
        [Display(Name = "Điểm yêu cầu")]
        public int MinPoints { get; set; } = 0;

        [Column("PhanTramGiamGia")]
        [Display(Name = "Giảm giá vé (%)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column("MoTa")]
        [Display(Name = "Mô tả quyền lợi")]
        public string? Description { get; set; }

        [Column("MauHienThi")]
        [Display(Name = "Màu huy hiệu")]
        public string BadgeColor { get; set; } = "#757575";

        // Navigation
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}

