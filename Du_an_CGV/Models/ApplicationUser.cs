using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Du_an_CGV.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? FavoriteCinema { get; set; }
        public string MemberTier { get; set; } = "Thành viên Thường";
        public int? MemberTierId { get; set; }
        public MemberTier? MemberTierRef { get; set; }
        public int RewardPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
