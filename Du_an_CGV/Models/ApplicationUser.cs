using Microsoft.AspNetCore.Identity;

namespace Du_an_CGV.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? FavoriteCinema { get; set; }
        public string MemberTier { get; set; } = "Standard Member";
        public int RewardPoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
