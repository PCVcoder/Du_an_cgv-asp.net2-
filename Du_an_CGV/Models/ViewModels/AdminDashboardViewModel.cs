using Du_an_CGV.Models;

namespace Du_an_CGV.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Statistics
        public int TotalNowShowingMovies { get; set; }
        public int TotalUpcomingMovies { get; set; }
        public int TotalEndedMovies { get; set; }
        public int TotalCinemas { get; set; }
        public int TotalShowtimes { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActivePromosCount { get; set; }
        public int NewInquiriesCount { get; set; }
        public int NewRentalsCount { get; set; }
        public int TotalFoodItems { get; set; }
        public int TotalActionLogsCount { get; set; }

        // Data collections
        public List<Movie> Movies { get; set; } = new();
        public List<Cinema> Cinemas { get; set; } = new();
        public List<Showtime> Showtimes { get; set; } = new();
        public List<NewsOffer> Promos { get; set; } = new();
        public List<ContactInquiry> Inquiries { get; set; } = new();
        public List<HallRentalRequest> Rentals { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();
        public List<Booking> AllBookings { get; set; } = new();
        public List<FoodItem> FoodItems { get; set; } = new();
        public List<AdminActionLog> ActionLogs { get; set; } = new();
        public List<string> Provinces { get; set; } = new();
        public List<MemberTier> MemberTiers { get; set; } = new();
        public List<SeatHold> ActiveSeatHolds { get; set; } = new();
    }
}
