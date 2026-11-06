using Du_an_CGV.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<CinemaHall> CinemaHalls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Showtime> Showtimes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<NewsOffer> NewsOffers { get; set; }
        public DbSet<ContactInquiry> ContactInquiries { get; set; }
        public DbSet<HallRentalRequest> HallRentalRequests { get; set; }
        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<BookingFoodItem> BookingFoodItems { get; set; }
        public DbSet<AdminActionLog> AdminActionLogs { get; set; }
        public DbSet<MemberTier> MemberTiers { get; set; }
        public DbSet<SeatHold> SeatHolds { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision
            builder.Entity<FoodItem>()
                .Property(f => f.Price)
                .HasPrecision(18, 2);

            builder.Entity<BookingFoodItem>()
                .Property(bf => bf.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Seat>()
                .Property(s => s.PriceModifier)
                .HasPrecision(18, 2);

            builder.Entity<Showtime>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            builder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            // Cascade configurations
            builder.Entity<CinemaHall>()
                .HasOne(ch => ch.Cinema)
                .WithMany(c => c.CinemaHalls)
                .HasForeignKey(ch => ch.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Seat>()
                .HasOne(s => s.CinemaHall)
                .WithMany(ch => ch.Seats)
                .HasForeignKey(s => s.CinemaHallId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Showtime>()
                .HasOne(st => st.Movie)
                .WithMany(m => m.Showtimes)
                .HasForeignKey(st => st.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Showtime>()
                .HasOne(st => st.CinemaHall)
                .WithMany(ch => ch.Showtimes)
                .HasForeignKey(st => st.CinemaHallId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                .HasOne(b => b.Showtime)
                .WithMany(st => st.Bookings)
                .HasForeignKey(b => b.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // Cấu hình liên kết chặt chẽ cho 4 bảng:
            // ==========================================

            // 1. BookingFoodItem: Liên kết giữa Booking và FoodItem
            builder.Entity<BookingFoodItem>()
                .HasOne(bf => bf.Booking)
                .WithMany(b => b.BookingFoodItems)
                .HasForeignKey(bf => bf.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookingFoodItem>()
                .HasOne(bf => bf.FoodItem)
                .WithMany(f => f.BookingFoodItems)
                .HasForeignKey(bf => bf.FoodItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. HallRentalRequest: Liên kết với Cinema và User
            builder.Entity<HallRentalRequest>()
                .HasOne(r => r.Cinema)
                .WithMany(c => c.HallRentalRequests)
                .HasForeignKey(r => r.CinemaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<HallRentalRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 3. ContactInquiry: Liên kết với User
            builder.Entity<ContactInquiry>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 4. NewsOffer: Liên kết với Movie, Cinema và Quản trị viên đăng bài (Author)
            builder.Entity<NewsOffer>()
                .HasOne(n => n.Movie)
                .WithMany(m => m.NewsOffers)
                .HasForeignKey(n => n.MovieId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<NewsOffer>()
                .HasOne(n => n.Cinema)
                .WithMany(c => c.NewsOffers)
                .HasForeignKey(n => n.CinemaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<NewsOffer>()
                .HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            // 5. AdminActionLog: Liên kết với Cinema (nullable)
            builder.Entity<AdminActionLog>()
                .HasOne(a => a.Cinema)
                .WithMany()
                .HasForeignKey(a => a.CinemaId)
                .OnDelete(DeleteBehavior.SetNull);

            // 6. MemberTier (CAPBAC_THANHVIEN) & ApplicationUser
            builder.Entity<MemberTier>()
                .Property(m => m.DiscountPercent)
                .HasPrecision(5, 2);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.MemberTierRef)
                .WithMany(mt => mt.Users)
                .HasForeignKey(u => u.MemberTierId)
                .OnDelete(DeleteBehavior.SetNull);

            // 7. SeatHold (GIUCHO)
            builder.Entity<SeatHold>()
                .HasOne(sh => sh.Showtime)
                .WithMany()
                .HasForeignKey(sh => sh.ShowtimeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SeatHold>()
                .HasOne(sh => sh.User)
                .WithMany()
                .HasForeignKey(sh => sh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

