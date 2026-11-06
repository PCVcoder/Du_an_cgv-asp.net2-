using Du_an_CGV.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Fast path: Ensure FoodItems table, full Cinemas list, and Showtimes exist even if movies exist
            if (await context.Database.CanConnectAsync() && await context.Movies.AnyAsync())
            {
                await EnsureFoodItemsTableAndSeedAsync(context);
                await EnsureCinemasSeededAsync(context);
                await EnsureAllMoviesHaveShowtimesAsync(context);
                await EnsureAdminActionLogsAndSampleBookingsAsync(context);
                await EnsureMemberTiersAndSeatHoldsTableAsync(context, userManager);
                await EnsureSoftDeleteAndDetailedRentalColumnsAsync(context);
                return;
            }

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = ["Admin", "Customer"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@cgv.vn";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên CGV",
                    EmailConfirmed = true,
                    PhoneNumber = "19006017",
                    City = "Hồ Chí Minh",
                    FavoriteCinema = "CGV Landmark 81",
                    MemberTier = "VIP Manager",
                    RewardPoints = 9999
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Normal Customer User
            var customerEmail = "khachhang@cgv.vn";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FullName = "Nguyễn Văn Khách",
                    EmailConfirmed = true,
                    PhoneNumber = "0901234567",
                    City = "Hồ Chí Minh",
                    FavoriteCinema = "CGV Landmark 81",
                    MemberTier = "Standard Member",
                    RewardPoints = 120
                };
                var result = await userManager.CreateAsync(customerUser, "Customer@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }

            // Also seed alias customer@cgv.vn for easy testing
            var customerAliasEmail = "customer@cgv.vn";
            var customerAlias = await userManager.FindByEmailAsync(customerAliasEmail);
            if (customerAlias == null)
            {
                customerAlias = new ApplicationUser
                {
                    UserName = customerAliasEmail,
                    Email = customerAliasEmail,
                    FullName = "Nguyễn Văn Khách (Test VIP)",
                    EmailConfirmed = true,
                    PhoneNumber = "0909888999",
                    City = "Hồ Chí Minh",
                    FavoriteCinema = "CGV Landmark 81",
                    MemberTier = "VIP",
                    RewardPoints = 2500
                };
                var result = await userManager.CreateAsync(customerAlias, "Customer@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerAlias, "Customer");
                }
            }

            // 4. Seed Cinemas
            if (!await context.Cinemas.AnyAsync())
            {
                var cinemas = new List<Cinema>
                {
                    new Cinema
                    {
                        Name = "CGV Vincom Center Landmark 81",
                        City = "Hồ Chí Minh",
                        Address = "Tầng B1, TTTM Vincom Center Landmark 81, 772 Điện Biên Phủ, Phường 22, Bình Thạnh, TP.HCM",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600",
                        Description = "Cụm rạp hiện đại bậc nhất Việt Nam với màn hình IMAX Laser lớn nhất khu vực."
                    },
                    new Cinema
                    {
                        Name = "CGV Sư Vạn Hạnh",
                        City = "Hồ Chí Minh",
                        Address = "Tầng 6, Vạn Hạnh Mall, 11 Sư Vạn Hạnh, Phường 12, Quận 10, TP.HCM",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600",
                        Description = "Tọa lạc tại trung tâm sầm uất với phòng chiếu IMAX và Gold Class sang trọng."
                    },
                    new Cinema
                    {
                        Name = "CGV Vincom Đồng Khởi",
                        City = "Hồ Chí Minh",
                        Address = "Tầng 3, TTTM Vincom Center Đồng Khởi, 72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600",
                        Description = "Rạp chiếu trung tâm Quận 1 cao cấp, đầy đủ trải nghiệm Gold Class và L'Amour."
                    },
                    new Cinema
                    {
                        Name = "CGV Crescent Mall",
                        City = "Hồ Chí Minh",
                        Address = "Lầu 5, TTTM Crescent Mall, Đại lộ Nguyễn Văn Linh, Phú Mỹ Hưng, Quận 7, TP.HCM",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600",
                        Description = "Khu phức hợp giải trí điện ảnh đẳng cấp khu vực Nam Sài Gòn."
                    },
                    new Cinema
                    {
                        Name = "CGV Hùng Vương Plaza",
                        City = "Hồ Chí Minh",
                        Address = "Tầng 7, Hùng Vương Plaza, 126 Hồng Bàng, Phường 12, Quận 5, TP.HCM",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600",
                        Description = "Rạp chiếu phim huyền thoại được nâng cấp toàn diện với phòng chiếu hiện đại."
                    },
                    new Cinema
                    {
                        Name = "CGV Vincom Bà Triệu",
                        City = "Hà Nội",
                        Address = "Tầng 6, Vincom Center Hà Nội, 191 Bà Triệu, Quận Hai Bà Trưng, Hà Nội",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600",
                        Description = "Rạp chiếu hàng đầu thủ đô với chất lượng dịch vụ chuẩn quốc tế."
                    },
                    new Cinema
                    {
                        Name = "CGV Vincom Royal City",
                        City = "Hà Nội",
                        Address = "Tầng B2, TTTM Vincom Megamall Royal City, 72A Nguyễn Trãi, Thanh Xuân, Hà Nội",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600",
                        Description = "Cụm rạp quy mô lớn dưới lòng đất với 10 phòng chiếu chất lượng cao."
                    },
                    new Cinema
                    {
                        Name = "CGV Vincom Đà Nẵng",
                        City = "Đà Nẵng",
                        Address = "Tầng 4, TTTM Vincom Plaza Ngô Quyền, 910A Ngô Quyền, Sơn Trà, Đà Nẵng",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600",
                        Description = "Điểm hẹn điện ảnh số 1 của khán giả thành phố biển Đà Nẵng."
                    },
                    new Cinema
                    {
                        Name = "CGV Sense City Cần Thơ",
                        City = "Cần Thơ",
                        Address = "Tầng 3, TTTM Sense City, 01 Đại lộ Hòa Bình, Ninh Kiều, Cần Thơ",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600",
                        Description = "Cụm rạp tiêu chuẩn quốc tế hiện đại bậc nhất khu vực Tây Nam Bộ."
                    },
                    new Cinema
                    {
                        Name = "CGV Vincom Hải Phòng",
                        City = "Hải Phòng",
                        Address = "Tầng 4, TTTM Vincom Plaza Lê Thánh Tông, 1 Lê Thánh Tông, Ngô Quyền, Hải Phòng",
                        Hotline = "1900 6017",
                        ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600",
                        Description = "Trải nghiệm điện ảnh đỉnh cao cho người dân thành phố hoa phượng đỏ."
                    }
                };

                await context.Cinemas.AddRangeAsync(cinemas);
                await context.SaveChangesAsync();

                // Seed Halls and Seats for the first few cinemas
                foreach (var cinema in cinemas)
                {
                    var halls = new List<CinemaHall>
                    {
                        new CinemaHall { CinemaId = cinema.Id, Name = "Cinema 01 (Digital 2D)", HallType = "Standard 2D", TotalSeats = 60 },
                        new CinemaHall { CinemaId = cinema.Id, Name = "Cinema 02 (3D Atmos)", HallType = "3D Atmos", TotalSeats = 60 },
                        new CinemaHall { CinemaId = cinema.Id, Name = "Cinema 03 (IMAX Laser)", HallType = "IMAX", TotalSeats = 60 },
                        new CinemaHall { CinemaId = cinema.Id, Name = "Cinema 04 (L'Amour Giường Nằm)", HallType = "L'Amour", TotalSeats = 24 }
                    };
                    await context.CinemaHalls.AddRangeAsync(halls);
                    await context.SaveChangesAsync();

                    // Seed standard seats for Cinema 01 & 03
                    string[] rows = ["A", "B", "C", "D", "E"];
                    foreach (var hall in halls.Take(2))
                    {
                        var seats = new List<Seat>();
                        foreach (var row in rows)
                        {
                            for (int i = 1; i <= 12; i++)
                            {
                                var seatType = (row == "C" || row == "D") ? "VIP" : "Standard";
                                seats.Add(new Seat
                                {
                                    CinemaHallId = hall.Id,
                                    Row = row,
                                    Number = i,
                                    SeatCode = $"{row}{i}",
                                    SeatType = seatType,
                                    PriceModifier = seatType == "VIP" ? 15000 : 0
                                });
                            }
                        }
                        await context.Seats.AddRangeAsync(seats);
                    }
                    await context.SaveChangesAsync();
                }
            }

            // 5. Seed Movies
            if (!await context.Movies.AnyAsync())
            {
                var movies = new List<Movie>
                {
                    new Movie
                    {
                        Title = "Lật Mặt 7: Một Điều Ước",
                        Genre = "Gia đình, Tâm lý",
                        Duration = 121,
                        Rating = "T16",
                        RatingColor = "#2e7d32",
                        ReleaseDate = new DateTime(2026, 4, 26),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1568038479111-87bf80659645?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        BannerUrl = "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=1200",
                        TrailerUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                        Director = "Lý Hải",
                        Cast = "Thanh Hiền, Trương Minh Cường, Đinh Y Nhung, Quách Ngọc Tuyên, Trâm Anh",
                        Language = "Tiếng Việt - Phụ đề Tiếng Anh",
                        Formats = "2D DIGITAL, IMAX",
                        Synopsis = "Câu chuyện cảm động về một người mẹ đơn thân tảo tần nuôi dạy 5 người con khôn lớn. Khi bà gặp tai nạn và già yếu, câu hỏi ai sẽ là người chăm sóc mẹ trở thành nỗi trăn trở sâu sắc của từng gia đình nhỏ.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = 1,
                        SpecialTag = null
                    },
                    new Movie
                    {
                        Title = "Vùng Bắt Quỷ Dữ",
                        Genre = "Hành động, Kinh dị, Giật gân",
                        Duration = 94,
                        Rating = "T18",
                        RatingColor = "#b71c1c",
                        ReleaseDate = new DateTime(2026, 5, 10),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1778973812223-e5079431fa47?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        BannerUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1200",
                        TrailerUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                        Director = "James Wan",
                        Cast = "Patrick Wilson, Vera Farmiga, Sterling Jerins",
                        Language = "Tiếng Anh - Phụ đề Tiếng Việt",
                        Formats = "2D DIGITAL, IMAX",
                        Synopsis = "Một cuộc trừ tà nghẹt thở giữa các chuyên gia huyền bí và ác quỷ cổ xưa giam hãm một thị trấn hoang vắng.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = null,
                        SpecialTag = "THE NEAK SHOW"
                    },
                    new Movie
                    {
                        Title = "Tế Vật Cải Mệnh",
                        Genre = "Kinh dị, Hồi hộp",
                        Duration = 103,
                        Rating = "T18",
                        RatingColor = "#b71c1c",
                        ReleaseDate = new DateTime(2026, 5, 15),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1651627567991-e4ec7b8fc72c?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Kim Sung-hoon",
                        Cast = "Choi Woo-shik, Kim Da-mi",
                        Language = "Tiếng Hàn - Phụ đề Tiếng Việt",
                        Formats = "2D DIGITAL",
                        Synopsis = "Nghi thức cải mệnh cổ xưa đòi hỏi một cái giá đắt đỏ bằng máu và linh hồn của những kẻ dám thách thức định mệnh.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = null,
                        SpecialTag = "THE NEAK SHOW"
                    },
                    new Movie
                    {
                        Title = "Kỳ Nghỉ Hè Nhớ Đời",
                        Genre = "Gia đình, Hài hước, Phiêu lưu",
                        Duration = 113,
                        Rating = "T13",
                        RatingColor = "#e65100",
                        ReleaseDate = new DateTime(2026, 6, 1),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1609062757924-6c2d01b3b422?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Nguyễn Quang Dũng",
                        Cast = "Thu Trang, Tiến Luật, Kiều Minh Tuấn",
                        Language = "Tiếng Việt",
                        Formats = "2D DIGITAL",
                        Synopsis = "Chuyến đi biển tràn ngập tiếng cười và những tình huống dở khóc dở cười của đại gia đình ba thế hệ.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = null
                    },
                    new Movie
                    {
                        Title = "Hope: Vùng Tử Địa",
                        Genre = "Khoa học viễn tưởng, Hành động",
                        Duration = 137,
                        Rating = "T16",
                        RatingColor = "#2e7d32",
                        ReleaseDate = new DateTime(2026, 6, 10),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1634733049839-0292be607569?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Na Hong-jin",
                        Cast = "Hwang Jung-min, Zo In-sung, Jung Ho-yeon",
                        Language = "Tiếng Hàn - Phụ đề Tiếng Việt",
                        Formats = "IMAX, 2D DIGITAL",
                        Synopsis = "Cư dân của một ngôi làng ven biển hẻo lánh phải đối mặt với một phát hiện bí ẩn đe dọa sự sinh tồn của loài người.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = 2,
                        SpecialTag = "IMAX"
                    },
                    new Movie
                    {
                        Title = "Quý Tử Vượt Giàu Bần Đói Khổ",
                        Genre = "Hoạt hình, Phiêu lưu, Hài",
                        Duration = 110,
                        Rating = "K",
                        RatingColor = "#1565c0",
                        ReleaseDate = new DateTime(2026, 6, 15),
                        EndDate = new DateTime(2026, 9, 30),
                        PosterUrl = "https://images.unsplash.com/photo-1581833971358-2c8b550f87b3?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Makoto Shinkai",
                        Cast = "Lồng tiếng Việt / Nhật",
                        Language = "Lồng tiếng Tiếng Việt",
                        Formats = "2D DIGITAL",
                        Synopsis = "Chàng công tử kiêu ngạo vô tình lạc vào vương quốc kỳ lạ và phải học cách sinh tồn từ hai bàn tay trắng.",
                        IsNowShowing = true,
                        IsUpcoming = false,
                        Rank = 3
                    },
                    new Movie
                    {
                        Title = "Marnie Yêu Dấu",
                        Genre = "Hoạt hình, Tâm lý",
                        Duration = 103,
                        Rating = "K",
                        RatingColor = "#1565c0",
                        ReleaseDate = new DateTime(2026, 11, 15),
                        PosterUrl = "https://images.unsplash.com/photo-1563170446-9c3c0622d8a9?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Hiromasa Yonebayashi",
                        Language = "Lồng tiếng & Phụ đề",
                        Formats = "2D DIGITAL",
                        Synopsis = "Tình bạn dịu dàng vượt thời gian giữa cô bé Anna khép kín và Marnie - cô gái bí ẩn ở ngôi biệt thự đầm lầy.",
                        IsNowShowing = false,
                        IsUpcoming = true
                    },
                    new Movie
                    {
                        Title = "Avengers: Hồi Kết - Phiên Bản Đặc Biệt",
                        Genre = "Hành động, Khoa học viễn tưởng",
                        Duration = 181,
                        Rating = "T13",
                        RatingColor = "#e65100",
                        ReleaseDate = new DateTime(2026, 12, 1),
                        PosterUrl = "https://images.unsplash.com/photo-1754677315657-b6c9cf2650ef?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400",
                        Director = "Anthony Russo, Joe Russo",
                        Cast = "Robert Downey Jr., Chris Evans, Chris Hemsworth, Scarlett Johansson",
                        Language = "Tiếng Anh - Phụ đề Tiếng Việt",
                        Formats = "IMAX, 3D DIGITAL",
                        Synopsis = "Trận chiến vĩ đại nhất của các Siêu anh hùng Marvel trở lại rạp chiếu với các thước phim chưa từng công bố.",
                        IsNowShowing = false,
                        IsUpcoming = true,
                        SpecialTag = "IMAX"
                    }
                };

                await context.Movies.AddRangeAsync(movies);
                await context.SaveChangesAsync();
            }

            // 6. Seed Showtimes
            if (!await context.Showtimes.AnyAsync())
            {
                var movies = await context.Movies.Where(m => m.IsNowShowing).ToListAsync();
                var halls = await context.CinemaHalls.Include(h => h.Cinema).Take(6).ToListAsync();

                var times = new[] { "10:15", "13:30", "16:45", "19:20", "21:50" };
                var showtimes = new List<Showtime>();

                foreach (var movie in movies.Take(4))
                {
                    foreach (var hall in halls.Take(2))
                    {
                        foreach (var timeStr in times)
                        {
                            var parts = timeStr.Split(':');
                            var hour = int.Parse(parts[0]);
                            var minute = int.Parse(parts[1]);

                            var start = DateTime.Today.AddHours(hour).AddMinutes(minute);
                            var end = start.AddMinutes(movie.Duration);

                            showtimes.Add(new Showtime
                            {
                                MovieId = movie.Id,
                                CinemaHallId = hall.Id,
                                StartTime = start,
                                EndTime = end,
                                Price = hall.HallType.Contains("IMAX") ? 160000 : 110000,
                                Format = hall.HallType.Contains("IMAX") ? "IMAX" : "2D Digital"
                            });
                        }
                    }
                }

                await context.Showtimes.AddRangeAsync(showtimes);
                await context.SaveChangesAsync();
            }

            // 7. Seed News & Offers
            if (!await context.NewsOffers.AnyAsync())
            {
                var offers = new List<NewsOffer>
                {
                    new NewsOffer
                    {
                        Title = "CGVXCOCA-COLA: DOUBLE DEAL – NHÂN ĐÔI BẮP NƯỚC",
                        Category = "offers",
                        CategoryLabel = "ƯU ĐÃI",
                        DateRange = "18/09/2026 - 01/10/2026",
                        ImageUrl = "https://images.unsplash.com/photo-1568702846914-96b305d2aaeb?w=600&q=80",
                        Summary = "Chỉ 249K mà có tới 5 món và tặng thêm bắp nước?! Mua DOUBLE DEAL COMBO ngay hôm nay tại CGV!",
                        Content = "Chương trình áp dụng tại tất cả các cụm rạp CGV trên toàn quốc khi mua combo đôi bắp nước cùng vé xem phim.",
                        IsActive = true
                    },
                    new NewsOffer
                    {
                        Title = "SƯU TẬP 5 MẢNH KÝ ỨC ENDGAME – TẶNG 1 POSTER",
                        Category = "news",
                        CategoryLabel = "TIN MỚI",
                        DateRange = "24/09/2026 - 02/10/2026",
                        ImageUrl = "https://images.unsplash.com/photo-1635805737707-575885ab0820?w=600&q=80",
                        Summary = "Sưu tập 5 mảnh ký ức Avengers Endgame và nhận ngay 1 Poster siêu to khổng lồ bản giới hạn – chỉ có tại CGV!",
                        Content = "Khách hàng mua vé xem phim IMAX hoặc mua combo đặc biệt sẽ nhận được 1 mảnh ghép. Thu thập đủ 5 mảnh đổi quà tại quầy vé.",
                        IsActive = true
                    },
                    new NewsOffer
                    {
                        Title = "CGV MEMBER DAY – COUPON GIẢM 99.000Đ",
                        Category = "member",
                        CategoryLabel = "THÀNH VIÊN",
                        DateRange = "Thứ Tư hàng tuần",
                        ImageUrl = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=600&q=80",
                        Summary = "X2 điểm thưởng và nhận Coupon giảm 99.000đ dành riêng cho thành viên CGV trong ngày Member Day.",
                        Content = "Dành riêng cho thành viên có thẻ CGV VIP và VVIP. Đăng nhập để nhận mã quà tặng ngay trên ứng dụng.",
                        IsActive = true
                    },
                    new NewsOffer
                    {
                        Title = "BÁNH TRÁNG TRỘN – SẢN PHẨM MỚI CHỈ 25.000Đ",
                        Category = "news",
                        CategoryLabel = "TIN MỚI",
                        DateRange = "Đang áp dụng",
                        ImageUrl = "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600&q=80",
                        Summary = "Cine De Chef ra mắt sản phẩm mới – Bánh Tráng Trộn với nhiều vị: Rong Biển, Mala, Tê Cay chỉ 25.000đ.",
                        Content = "Thưởng thức món ăn vặt quốc dân ngay trong rạp chiếu phim với tiêu chuẩn vệ sinh an toàn thực phẩm hàng đầu.",
                        IsActive = true
                    },
                    new NewsOffer
                    {
                        Title = "🎉 THỨ TƯ VUI VẺ - VÉ ĐỒNG GIÁ TỪ 65K TẠI CGV TOÀN QUỐC",
                        Category = "offers",
                        CategoryLabel = "ƯU ĐÃI",
                        DateRange = "Đến hết 31/12/2026",
                        ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600",
                        Summary = "Thứ Tư hàng tuần, xem phim thả ga với giá vé chỉ từ 65.000đ cho mọi lứa tuổi và tất cả các suất chiếu.",
                        Content = "Áp dụng cho mọi suất chiếu 2D vào ngày Thứ Tư tại tất cả các cụm rạp CGV trên toàn quốc.",
                        IsActive = true
                    }
                };

                await context.NewsOffers.AddRangeAsync(offers);
                await context.SaveChangesAsync();
            }

            // 8. Seed Contact Inquiries
            if (!await context.ContactInquiries.AnyAsync())
            {
                var inquiries = new List<ContactInquiry>
                {
                    new ContactInquiry
                    {
                        FullName = "Nguyễn Văn An",
                        Email = "an.nguyen@gmail.com",
                        PhoneNumber = "0988123456",
                        Subject = "Hỏi về đặt vé nhóm 50 người tại CGV Landmark 81",
                        Message = "Công ty tôi có khoảng 50 nhân viên muốn đặt vé xem Lật Mặt 7 vào cuối tuần này. Nhờ CGV tư vấn báo giá và ưu đãi bắp nước.",
                        Status = "Chưa đọc",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new ContactInquiry
                    {
                        FullName = "Phạm Thị Lan",
                        Email = "lanpham@yahoo.com",
                        PhoneNumber = "0912345678",
                        Subject = "Góp ý về chất lượng âm thanh rạp chiếu số 3",
                        Message = "Hôm qua mình xem suất chiếu 19h tại phòng 3, âm thanh loa vòm phía sau có tiếng rè nhỏ, mong ban quản lý kiểm tra lại thiết bị.",
                        Status = "Chưa đọc",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new ContactInquiry
                    {
                        FullName = "Trần Thị Bích",
                        Email = "bich.tran@outlook.com",
                        PhoneNumber = "0934567890",
                        Subject = "Thắc mắc về điểm tích lũy thẻ thành viên CGV VIP",
                        Message = "Tôi vừa đổi thẻ sang hạng VIP nhưng chưa thấy số điểm thưởng sinh nhật tháng này được cộng vào tài khoản.",
                        Status = "Đã đọc",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        AdminNotes = "Đã kiểm tra hệ thống, điểm thưởng sẽ tự động kích hoạt vào ngày đầu tháng sinh nhật của khách."
                    },
                    new ContactInquiry
                    {
                        FullName = "Lê Minh Khoa",
                        Email = "khoa.le@gmail.com",
                        PhoneNumber = "0977889900",
                        Subject = "Thuê trọn phòng chiếu L'Amour tổ chức sự kiện cầu hôn",
                        Message = "Mình muốn thuê trọn phòng L'Amour Landmark 81 trong 2 tiếng để chiếu clip và cầu hôn bạn gái. Xin báo giá gói dịch vụ.",
                        Status = "Đã phản hồi",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        AdminNotes = "Đã gọi điện tư vấn trực tiếp và gửi báo giá gói L'Amour Premium qua email."
                    }
                };

                await context.ContactInquiries.AddRangeAsync(inquiries);
                await context.SaveChangesAsync();
            }

            // 9. Seed Hall Rental Requests
            if (!await context.HallRentalRequests.AnyAsync())
            {
                var rentals = new List<HallRentalRequest>
                {
                    new HallRentalRequest
                    {
                        FullName = "Võ Hoàng Nam",
                        CompanyName = "Tập đoàn VNG Corporation",
                        Email = "namvh@vng.com.vn",
                        PhoneNumber = "0909887766",
                        CinemaName = "CGV Vincom Center Landmark 81",
                        ExpectedDate = DateTime.Today.AddDays(14),
                        GuestCount = 120,
                        ServiceType = "Thuê trọn rạp tổ chức sự kiện",
                        Notes = "Tổ chức hội thảo nội bộ công ty kết hợp xem phim tri ân nhân viên.",
                        Status = "Mới tiếp nhận"
                    },
                    new HallRentalRequest
                    {
                        FullName = "Đặng Thùy Trang",
                        CompanyName = "Trường THPT Lê Hồng Phong",
                        Email = "trang.dang@lhp.edu.vn",
                        PhoneNumber = "0918273645",
                        CinemaName = "CGV Sư Vạn Hạnh",
                        ExpectedDate = DateTime.Today.AddDays(7),
                        GuestCount = 80,
                        ServiceType = "Đặt vé xem phim theo nhóm",
                        Notes = "Buổi xem phim ngoại khóa môn Lịch sử - Ngữ văn cho học sinh khối 11.",
                        Status = "Đã liên hệ"
                    }
                };

                await context.HallRentalRequests.AddRangeAsync(rentals);
                await context.SaveChangesAsync();
            }

            // 10. Seed Sample Bookings
            if (!await context.Bookings.AnyAsync())
            {
                var firstShowtime = await context.Showtimes.Include(s => s.Movie).FirstOrDefaultAsync();
                if (firstShowtime != null && customerUser != null)
                {
                    var bookings = new List<Booking>
                    {
                        new Booking
                        {
                            BookingCode = "CGV-20261014-9981",
                            UserId = customerUser.Id,
                            CustomerName = customerUser.FullName,
                            CustomerEmail = customerUser.Email!,
                            CustomerPhone = customerUser.PhoneNumber ?? "0901234567",
                            ShowtimeId = firstShowtime.Id,
                            BookingDate = DateTime.UtcNow.AddHours(-3),
                            SeatsList = "C4, C5",
                            TotalAmount = firstShowtime.Price * 2 + 30000,
                            PaymentMethod = "VNPAY-QR",
                            Status = "Đã thanh toán"
                        },
                        new Booking
                        {
                            BookingCode = "CGV-20261014-4523",
                            UserId = null,
                            CustomerName = "Trần Quốc Huy",
                            CustomerEmail = "huy.tran@gmail.com",
                            CustomerPhone = "0944556677",
                            ShowtimeId = firstShowtime.Id,
                            BookingDate = DateTime.UtcNow.AddHours(-1),
                            SeatsList = "D6, D7",
                            TotalAmount = firstShowtime.Price * 2 + 30000,
                            PaymentMethod = "ZaloPay",
                            Status = "Đã thanh toán"
                        }
                    };

                    await context.Bookings.AddRangeAsync(bookings);
                    await context.SaveChangesAsync();
                }
            }

            await EnsureFoodItemsTableAndSeedAsync(context);
            await EnsureAdminActionLogsAndSampleBookingsAsync(context);
            await EnsureMemberTiersAndSeatHoldsTableAsync(context, null);
            await EnsureSoftDeleteAndDetailedRentalColumnsAsync(context);
        }

        public static async Task EnsureFoodItemsTableAndSeedAsync(ApplicationDbContext context)
        {
            try
            {
                // Create table if missing (for databases created prior to FoodItem model)
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FoodItems')
                    BEGIN
                        CREATE TABLE [FoodItems] (
                            [Id] int NOT NULL IDENTITY,
                            [Name] nvarchar(max) NOT NULL,
                            [Category] nvarchar(max) NOT NULL,
                            [Price] decimal(18,2) NOT NULL,
                            [ImageUrl] nvarchar(max) NULL,
                            [Description] nvarchar(max) NULL,
                            [IsAvailable] bit NOT NULL,
                            [SortOrder] int NOT NULL DEFAULT 0,
                            CONSTRAINT [PK_FoodItems] PRIMARY KEY ([Id])
                        );
                    END
                ");

                if (!await context.FoodItems.AnyAsync())
                {
                    var foodItems = new List<FoodItem>
                    {
                        new FoodItem
                        {
                            Name = "CGV Combo (1 Bắp Ngọt Lớn + 2 Nước Ngọt 32oz)",
                            Category = "Combo",
                            Price = 115000,
                            ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                            Description = "01 Bắp rang bơ vị ngọt size Lớn giòn rụm + 02 Nước ngọt có ga tự chọn size lớn (Coca/Sprite/Fanta).",
                            IsAvailable = true,
                            SortOrder = 1
                        },
                        new FoodItem
                        {
                            Name = "My Combo (1 Bắp Ngọt Vừa + 1 Nước Ngọt 32oz)",
                            Category = "Combo",
                            Price = 89000,
                            ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500",
                            Description = "Dành riêng cho 1 người: 01 Bắp rang bơ vị ngọt size Vừa + 01 Nước ngọt có ga 32oz tự chọn.",
                            IsAvailable = true,
                            SortOrder = 2
                        },
                        new FoodItem
                        {
                            Name = "Golden Caramel Combo (1 Bắp Caramel Lớn + 2 Trà Sữa)",
                            Category = "Combo",
                            Price = 139000,
                            ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500",
                            Description = "01 Bắp Caramel thơm béo thượng hạng size Lớn + 02 Ly Trà sữa Nestea thơm mát chuẩn vị CGV.",
                            IsAvailable = true,
                            SortOrder = 3
                        },
                        new FoodItem
                        {
                            Name = "Double Deal Combo (2 Bắp Phô Mai Lớn + 2 Nước + 1 Snack)",
                            Category = "Combo",
                            Price = 169000,
                            ImageUrl = "https://images.unsplash.com/photo-1518133835878-5a93cc3f89e5?w=500",
                            Description = "Combo siêu hời cho nhóm bạn: 02 Bắp phô mai béo ngậy size Lớn + 02 Nước ngọt 32oz + 01 Snack khoai tây giòn cay.",
                            IsAvailable = true,
                            SortOrder = 4
                        },
                        new FoodItem
                        {
                            Name = "Super Premium VIP Combo (1 Bắp Mix + 2 Nước + 1 Hotdog)",
                            Category = "Combo",
                            Price = 185000,
                            ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3d5d6281729?w=500",
                            Description = "01 Bắp Mix 2 vị Phô Mai & Caramel + 02 Nước ngọt 32oz + 01 Hotdog Xúc Xích Phô Mai đặc biệt nóng hổi.",
                            IsAvailable = true,
                            SortOrder = 5
                        },
                        new FoodItem
                        {
                            Name = "Bắp Rang Bơ Truyền Thống (Size L)",
                            Category = "Bắp rang",
                            Price = 65000,
                            ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500",
                            Description = "Bắp rang bơ nguyên bản, bùi béo, thơm phức vừa ra lò.",
                            IsAvailable = true,
                            SortOrder = 6
                        },
                        new FoodItem
                        {
                            Name = "Bắp Rang Vị Phô Mai (Size L)",
                            Category = "Bắp rang",
                            Price = 79000,
                            ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                            Description = "Bắp rang phủ đều bột phô mai mặn mà đậm đà, bestseller tại tất cả rạp CGV.",
                            IsAvailable = true,
                            SortOrder = 7
                        },
                        new FoodItem
                        {
                            Name = "Bắp Rang Vị Caramel Ngọt Ngào (Size L)",
                            Category = "Bắp rang",
                            Price = 79000,
                            ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500",
                            Description = "Lớp sốt caramel vàng óng bao phủ từng hạt bắp giòn rụm ngọt dịu.",
                            IsAvailable = true,
                            SortOrder = 8
                        },
                        new FoodItem
                        {
                            Name = "Coca-Cola Tươi Mát Lạnh (32oz)",
                            Category = "Nước uống",
                            Price = 39000,
                            ImageUrl = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=500",
                            Description = "Ly nước ngọt Coca-Cola tươi mát lạnh sảng khoái với đá bi.",
                            IsAvailable = true,
                            SortOrder = 9
                        },
                        new FoodItem
                        {
                            Name = "Sprite Chanh Tươi Mát (32oz)",
                            Category = "Nước uống",
                            Price = 39000,
                            ImageUrl = "https://images.unsplash.com/photo-1625772299848-391b6a87d7b3?w=500",
                            Description = "Nước ngọt có ga vị chanh thanh mát, giải khát cực đã.",
                            IsAvailable = true,
                            SortOrder = 10
                        },
                        new FoodItem
                        {
                            Name = "Trà Đào Cam Sả Tươi Mát",
                            Category = "Nước uống",
                            Price = 49000,
                            ImageUrl = "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=500",
                            Description = "Trà đen ủ lạnh thơm nồng hương sả, cam tươi và miếng đào giòn ngọt.",
                            IsAvailable = true,
                            SortOrder = 11
                        },
                        new FoodItem
                        {
                            Name = "Hotdog Xúc Xích Phô Mai CGV",
                            Category = "Snack",
                            Price = 55000,
                            ImageUrl = "https://images.unsplash.com/photo-1619740455993-9e612b1af08a?w=500",
                            Description = "Bánh mì mềm kẹp xúc xích Đức xông khói nướng phô mai tan chảy sốt mù tạt mật ong.",
                            IsAvailable = true,
                            SortOrder = 12
                        },
                        new FoodItem
                        {
                            Name = "Khoai Tây Lắc Phô Mai Giòn Rụm",
                            Category = "Snack",
                            Price = 49000,
                            ImageUrl = "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=500",
                            Description = "Khoai tây chiên giòn rụm vàng ươm lắc đều bột phô mai thơm ngậy.",
                            IsAvailable = true,
                            SortOrder = 13
                        }
                    };

                    await context.FoodItems.AddRangeAsync(foodItems);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Silently handle if schema setup is locked or in-memory
            }
        }

        public static async Task GenerateSeatsForHallAsync(ApplicationDbContext context, int cinemaHallId, int rowCount = 5, int seatsPerRow = 12)
        {
            var existingSeats = await context.Seats.Where(s => s.CinemaHallId == cinemaHallId).ToListAsync();
            if (existingSeats.Any())
            {
                context.Seats.RemoveRange(existingSeats);
                await context.SaveChangesAsync();
            }

            char startChar = 'A';
            var seats = new List<Seat>();
            for (int r = 0; r < rowCount; r++)
            {
                string rowName = ((char)(startChar + r)).ToString();
                for (int num = 1; num <= seatsPerRow; num++)
                {
                    var isVip = (rowCount >= 4 && (r == 2 || r == 3)); // Hàng C, D là VIP
                    seats.Add(new Seat
                    {
                        CinemaHallId = cinemaHallId,
                        Row = rowName,
                        Number = num,
                        SeatCode = $"{rowName}{num}",
                        SeatType = isVip ? "VIP" : "Standard",
                        PriceModifier = isVip ? 15000 : 0
                    });
                }
            }
            await context.Seats.AddRangeAsync(seats);
            await context.SaveChangesAsync();
        }

        public static async Task EnsureCinemasSeededAsync(ApplicationDbContext context)
        {
            try
            {
                var fullCinemasList = new List<Cinema>
                {
                    // === 21 CỤM RẠP HỒ CHÍ MINH (THEO ĐÚNG HÌNH 4 CỦA NGƯỜI DÙNG) ===
                    new Cinema { Name = "CGV Hùng Vương Plaza", City = "Hồ Chí Minh", Address = "Tầng 7, Hùng Vương Plaza, 126 Hồng Bàng, Phường 12, Quận 5, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Rạp chiếu phim huyền thoại được nâng cấp toàn diện với phòng chiếu hiện đại." },
                    new Cinema { Name = "CGV Crescent Mall", City = "Hồ Chí Minh", Address = "Lầu 5, TTTM Crescent Mall, Đại lộ Nguyễn Văn Linh, Tân Phú, Quận 7, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Khu phức hợp giải trí điện ảnh đẳng cấp khu vực Nam Sài Gòn." },
                    new Cinema { Name = "CGV Thảo Điền Pearl", City = "Hồ Chí Minh", Address = "Tầng 2, Thảo Điền Pearl, 12 Quốc Hương, Phường Thảo Điền, TP. Thủ Đức, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Cụm rạp cao cấp phục vụ cư dân khu đô thị Thảo Điền." },
                    new Cinema { Name = "CGV Vincom Thủ Đức", City = "Hồ Chí Minh", Address = "Tầng 5, Vincom Plaza Thủ Đức, 216 Võ Văn Ngân, Bình Thọ, TP. Thủ Đức, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Điểm hẹn điện ảnh sôi động cho giới trẻ và sinh viên TP. Thủ Đức." },
                    new Cinema { Name = "CGV Vivo City", City = "Hồ Chí Minh", Address = "Tầng 5, SC VivoCity, 1058 Nguyễn Văn Linh, Tân Phong, Quận 7, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Rạp chiếu với màn hình IMAX Laser và phòng chiếu Gold Class cực sang trọng." },
                    new Cinema { Name = "CGV Pearl Plaza", City = "Hồ Chí Minh", Address = "Tầng 5, Pearl Plaza, 561A Điện Biên Phủ, Phường 25, Bình Thạnh, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Rạp chiếu trung tâm Bình Thạnh thuận tiện di chuyển." },
                    new Cinema { Name = "CGV Liberty Citypoint", City = "Hồ Chí Minh", Address = "Tầng M-1, Liberty Central Saigon Citypoint, 59-61 Pasteur, Quận 1, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Phòng chiếu VIP Premium đẳng cấp thượng lưu ngay tại Quận 1." },
                    new Cinema { Name = "CGV Vincom Đồng Khởi", City = "Hồ Chí Minh", Address = "Tầng 3, TTTM Vincom Center Đồng Khởi, 72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Rạp chiếu trung tâm Quận 1 cao cấp, đầy đủ trải nghiệm Gold Class và L'Amour." },
                    new Cinema { Name = "CGV Menas Mall (CGV CT Plaza)", City = "Hồ Chí Minh", Address = "Tầng 10, Menas Mall Saigon Airport, 60A Trường Sơn, Phường 2, Tân Bình, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Rạp đối diện sân bay Tân Sơn Nhất với view ngắm máy bay tuyệt đẹp." },
                    new Cinema { Name = "CGV Pandora City", City = "Hồ Chí Minh", Address = "Lầu 3, Pandora City, 1/1 Trường Chinh, Tây Thạnh, Tân Phú, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Quy mô 8 phòng chiếu hiện đại tại cửa ngõ Tây Bắc thành phố." },
                    new Cinema { Name = "CGV Aeon Tân Phú", City = "Hồ Chí Minh", Address = "Tầng 3, AEON Mall Tân Phú Celadon, 30 Bờ Bao Tân Thắng, Sơn Kỳ, Tân Phú, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Cụm rạp quy mô lớn bậc nhất khu vực Tân Phú với phòng chiếu IMAX hiện đại." },
                    new Cinema { Name = "CGV Vincom Gò Vấp", City = "Hồ Chí Minh", Address = "Tầng 5, Vincom Plaza Gò Vấp, 12 Phan Văn Trị, Phường 7, Gò Vấp, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Điểm hẹn giải trí hàng đầu khu vực Gò Vấp." },
                    new Cinema { Name = "CGV Hoàng Văn Thụ", City = "Hồ Chí Minh", Address = "Tầng 1 & 2, Gala Center, 415 Hoàng Văn Thụ, Phường 2, Tân Bình, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Rạp chiếu hiện đại với công nghệ phòng chiếu 4DX và màn hình Starium." },
                    new Cinema { Name = "CGV Aeon Bình Tân", City = "Hồ Chí Minh", Address = "Tầng 3, AEON Mall Bình Tân, Số 1 Đường Số 17A, Bình Trị Đông B, Bình Tân, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Tổ hợp giải trí điện ảnh hàng đầu khu vực phía Tây TP.HCM." },
                    new Cinema { Name = "CGV Saigonres Nguyễn Xí", City = "Hồ Chí Minh", Address = "Tầng 4-5, Saigonres Plaza, 79/81 Nguyễn Xí, Phường 26, Bình Thạnh, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Không gian rạp chiếu trẻ trung, năng động tại Bình Thạnh." },
                    new Cinema { Name = "CGV Sư Vạn Hạnh", City = "Hồ Chí Minh", Address = "Tầng 6, Vạn Hạnh Mall, 11 Sư Vạn Hạnh, Phường 12, Quận 10, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Tọa lạc tại trung tâm sầm uất với phòng chiếu IMAX và Gold Class sang trọng." },
                    new Cinema { Name = "CGV Vincom Center Landmark 81", City = "Hồ Chí Minh", Address = "Tầng B1, TTTM Vincom Center Landmark 81, 772 Điện Biên Phủ, Phường 22, Bình Thạnh, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Cụm rạp hiện đại bậc nhất Việt Nam với màn hình IMAX Laser lớn nhất khu vực." },
                    new Cinema { Name = "CGV Satra Củ Chi", City = "Hồ Chí Minh", Address = "Tầng 3, TTTM Satra Củ Chi, 1239 Tỉnh Lộ 8, Ấp Thạnh An, Xã Trung An, Củ Chi, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Cụm rạp hiện đại phục vụ đông đảo bà con khu vực ngoại thành Củ Chi." },
                    new Cinema { Name = "CGV Gigamall Thủ Đức", City = "Hồ Chí Minh", Address = "Tầng 6, TTTM Giga Mall, 240-242 Phạm Văn Đồng, Hiệp Bình Chánh, TP. Thủ Đức, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Cụm rạp quy mô cực lớn với sảnh chờ hoành tráng tại đại lộ Phạm Văn Đồng." },
                    new Cinema { Name = "CGV Lý Chính Thắng", City = "Hồ Chí Minh", Address = "Tầng 3, Tòa nhà Terra Royal, 280 Nam Kỳ Khởi Nghĩa / 83 Lý Chính Thắng, Phường 8, Quận 3, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Không gian điện ảnh cổ điển sang trọng giữa lòng Quận 3." },
                    new Cinema { Name = "CGV Vincom Mega Mall Grand Park", City = "Hồ Chí Minh", Address = "Tầng 4, Vincom Mega Mall Grand Park, KĐT Vinhomes Grand Park, Long Bình, TP. Thủ Đức, TP.HCM", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Cụm rạp hiện đại mới nhất phục vụ đại đô thị Vinhomes Grand Park." },

                    // === CÁC CỤM RẠP CÁC TỈNH / THÀNH PHỐ KHÁC ===
                    new Cinema { Name = "CGV Vincom Bà Triệu", City = "Hà Nội", Address = "Tầng 6, Vincom Center Hà Nội, 191 Bà Triệu, Quận Hai Bà Trưng, Hà Nội", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Rạp chiếu hàng đầu thủ đô với chất lượng dịch vụ chuẩn quốc tế." },
                    new Cinema { Name = "CGV Vincom Royal City", City = "Hà Nội", Address = "Tầng B2, TTTM Vincom Megamall Royal City, 72A Nguyễn Trãi, Thanh Xuân, Hà Nội", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Cụm rạp quy mô lớn dưới lòng đất với 10 phòng chiếu chất lượng cao." },
                    new Cinema { Name = "CGV Vincom Center Trần Duy Hưng", City = "Hà Nội", Address = "Tầng 5, Vincom Center Trần Duy Hưng, Cầu Giấy, Hà Nội", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Rạp chiếu thời thượng với công nghệ âm thanh Dolby Atmos." },
                    new Cinema { Name = "CGV Vincom Mega Mall Times City", City = "Hà Nội", Address = "Tầng B1, TTTM Vincom Mega Mall Times City, 458 Minh Khai, Hai Bà Trưng, Hà Nội", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Cụm rạp hiện đại bậc nhất khu đô thị Times City." },
                    new Cinema { Name = "CGV Aeon Mall Hà Đông", City = "Hà Nội", Address = "Tầng 3, AEON Mall Hà Đông, Dương Nội, Hà Đông, Hà Nội", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Màn hình IMAX khổng lồ phục vụ khu vực Tây Nam thủ đô." },
                    
                    new Cinema { Name = "CGV Vincom Đà Nẵng", City = "Đà Nẵng", Address = "Tầng 4, TTTM Vincom Plaza Ngô Quyền, 910A Ngô Quyền, Sơn Trà, Đà Nẵng", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Điểm hẹn điện ảnh số 1 của khán giả thành phố biển Đà Nẵng." },
                    new Cinema { Name = "CGV Vĩnh Trung Plaza", City = "Đà Nẵng", Address = "255-257 Hùng Vương, Vĩnh Trung, Thanh Khê, Đà Nẵng", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Rạp chiếu trung tâm Đà Nẵng phục vụ cộng đồng trẻ." },

                    new Cinema { Name = "CGV Sense City Cần Thơ", City = "Cần Thơ", Address = "Tầng 3, TTTM Sense City, 01 Đại lộ Hòa Bình, Ninh Kiều, Cần Thơ", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Cụm rạp tiêu chuẩn quốc tế hiện đại bậc nhất khu vực Tây Nam Bộ." },
                    new Cinema { Name = "CGV Vincom Hùng Vương Cần Thơ", City = "Cần Thơ", Address = "Số 2 Hùng Vương, Thới Bình, Ninh Kiều, Cần Thơ", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Rạp chiếu hiện đại tại Vincom Hùng Vương." },

                    new Cinema { Name = "CGV Vincom Hải Phòng", City = "Hải Phòng", Address = "Tầng 4, TTTM Vincom Plaza Lê Thánh Tông, 1 Lê Thánh Tông, Ngô Quyền, Hải Phòng", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Trải nghiệm điện ảnh đỉnh cao cho người dân thành phố hoa phượng đỏ." },
                    new Cinema { Name = "CGV Aeon Mall Hải Phòng Lê Chân", City = "Hải Phòng", Address = "Tầng 3, AEON Mall Lê Chân, Số 10 Võ Nguyên Giáp, Dư Hàng Kênh, Lê Chân, Hải Phòng", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Rạp chiếu với phòng chiếu IMAX quy mô lớn nhất Hải Phòng." },

                    new Cinema { Name = "CGV Vincom Biên Hòa", City = "Đồng Nai", Address = "Tầng 3, TTTM Vincom Plaza Biên Hòa, 1096 Phạm Văn Thuận, Biên Hòa, Đồng Nai", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Rạp chiếu trung tâm thành phố Biên Hòa sôi động." },
                    new Cinema { Name = "CGV Big C Đồng Nai", City = "Đồng Nai", Address = "Tầng trệt, Siêu thị Big C Đồng Nai, Khu phố 2, Long Bình Tân, Biên Hòa, Đồng Nai", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Cụm rạp quen thuộc của khán giả Đồng Nai." },

                    new Cinema { Name = "CGV Vincom Hạ Long", City = "Quảng Ninh", Address = "Tầng 4, TTTM Vincom Center Hạ Long, Cột Đồng Hồ, Bạch Đằng, Hạ Long, Quảng Ninh", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Rạp chiếu bờ vịnh Hạ Long kỳ quan." },
                    new Cinema { Name = "CGV Vincom Cẩm Phả", City = "Quảng Ninh", Address = "Tầng 3, Vincom Plaza Cẩm Phả, Cẩm Bình, Cẩm Phả, Quảng Ninh", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Cụm rạp hiện đại tại thành phố Cẩm Phả." },

                    new Cinema { Name = "CGV Lam Sơn Square Vũng Tàu", City = "Bà Rịa-Vũng Tàu", Address = "Tầng 4, Lam Sơn Square, 9 Lê Lợi, Phường 1, TP. Vũng Tàu", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Rạp chiếu trung tâm Bãi Trước thành phố Vũng Tàu." },

                    new Cinema { Name = "CGV Aeon Mall Bình Dương Canary", City = "Bình Dương", Address = "Tầng 2, AEON Mall Canary Bình Dương, Đại lộ Bình Dương, Thuận An, Bình Dương", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Tổ hợp rạp chiếu tiêu chuẩn cao tại Bình Dương." },
                    new Cinema { Name = "CGV Bình Dương Square", City = "Bình Dương", Address = "Tầng 3, Bình Dương Square, 01 Phú Lợi, Phú Hòa, Thủ Dầu Một, Bình Dương", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Rạp chiếu hiện đại tại Thủ Dầu Một." },

                    new Cinema { Name = "CGV Kim Cúc Plaza Quy Nhơn", City = "Bình Định", Address = "Tầng 3, Kim Cúc Plaza, Khu Đô Thị Vũng Chua, Ghềnh Ráng, Quy Nhơn, Bình Định", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Rạp chiếu hiện đại số 1 thành phố biển Quy Nhơn." },
                    new Cinema { Name = "CGV Buôn Ma Thuột", City = "Đắk Lắk", Address = "Tầng 5, Nguyễn Kim Buôn Ma Thuột, 01 Nguyễn Chí Thanh, Tân An, Buôn Ma Thuột", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600", Description = "Cụm rạp đẳng cấp tại thủ phủ cà phê Tây Nguyên." },
                    new Cinema { Name = "CGV Big C Nha Trang", City = "Khánh Hòa", Address = "Tầng trệt, TTTM Go! Nha Trang, Lô số 4, đường 19/5, Vĩnh Điềm Trung, Nha Trang", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1574267432553-4b4628081c31?w=600", Description = "Điểm hẹn điện ảnh số 1 thành phố biển Nha Trang." },
                    new Cinema { Name = "CGV Vinh Centre", City = "Nghệ An", Address = "Tầng 4, TTTM Vinh Centre, 69 Hồ Tùng Mậu, Trường Thi, TP. Vinh, Nghệ An", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=600", Description = "Rạp chiếu chuẩn quốc tế trung tâm thành phố Vinh." },
                    new Cinema { Name = "CGV Vincom Rạch Giá", City = "Kiên Giang", Address = "Tầng 4, TTTM Vincom Plaza Kiên Giang, Lô A12, Cô Bắc, Vĩnh Bảo, Rạch Giá, Kiên Giang", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1524712245354-2c4e5e7121c0?w=600", Description = "Rạp chiếu chuẩn quốc tế hiện đại tại Kiên Giang." },
                    new Cinema { Name = "CGV Vincom Mỹ Tho", City = "Tiền Giang", Address = "Tầng 4, Vincom Plaza Mỹ Tho, 1A Hùng Vương, Phường 1, TP. Mỹ Tho, Tiền Giang", Hotline = "1900 6017", ImageUrl = "https://images.unsplash.com/photo-1595769816263-9b910be24d5f?w=600", Description = "Cụm rạp phục vụ khán giả thành phố Mỹ Tho và Tiền Giang." }
                };

                foreach (var c in fullCinemasList)
                {
                    if (!await context.Cinemas.AnyAsync(existing => existing.Name == c.Name))
                    {
                        context.Cinemas.Add(c);
                        await context.SaveChangesAsync();

                        // Add 2 halls for this cinema
                        var halls = new List<CinemaHall>
                        {
                            new CinemaHall { CinemaId = c.Id, Name = "Cinema 01 (Digital 2D)", HallType = "Standard 2D", TotalSeats = 60 },
                            new CinemaHall { CinemaId = c.Id, Name = "Cinema 02 (3D Atmos)", HallType = "3D Atmos", TotalSeats = 60 }
                        };
                        context.CinemaHalls.AddRange(halls);
                        await context.SaveChangesAsync();

                        foreach (var h in halls)
                        {
                            await GenerateSeatsForHallAsync(context, h.Id, rowCount: 5, seatsPerRow: 12);
                        }
                    }
                }
            }
            catch
            {
                // Ignore if error occurs during concurrent seed
            }
        }

        public static async Task EnsureAllMoviesHaveShowtimesAsync(ApplicationDbContext context)
        {
            try
            {
                // 1. Đảm bảo toàn bộ phòng chiếu đều có ghế ngồi
                var hallsWithoutSeats = await context.CinemaHalls
                    .Where(h => !context.Seats.Any(s => s.CinemaHallId == h.Id))
                    .ToListAsync();

                foreach (var h in hallsWithoutSeats)
                {
                    if (h.HallType.Contains("L'Amour") || h.HallType.Contains("Lamour"))
                    {
                        await GenerateSeatsForHallAsync(context, h.Id, rowCount: 4, seatsPerRow: 6);
                    }
                    else
                    {
                        await GenerateSeatsForHallAsync(context, h.Id, rowCount: 5, seatsPerRow: 12);
                    }
                }

                // 2. Lấy danh sách toàn bộ phim ĐANG CHIẾU (loại bỏ phim sắp chiếu và ngưng chiếu)
                var nowShowingMovies = await context.Movies
                    .Where(m => m.IsNowShowing && !m.IsUpcoming)
                    .ToListAsync();

                if (!nowShowingMovies.Any()) return;

                // 3. Lấy TẤT CẢ các cụm rạp và toàn bộ phòng chiếu trên toàn quốc
                var allHalls = await context.CinemaHalls
                    .Include(h => h.Cinema)
                    .OrderBy(h => h.CinemaId)
                    .ThenBy(h => h.Id)
                    .ToListAsync();

                if (!allHalls.Any()) return;

                // 4. Lấy các suất chiếu đã có trong vòng 3 ngày tới để tránh trùng lịch
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(3);

                var existingShowtimes = await context.Showtimes
                    .Where(s => s.StartTime >= startDate && s.StartTime < endDate)
                    .ToListAsync();

                // Nếu đã có sẵn lịch chiếu cho 3 ngày tới, không cần tính toán và lặp lại hàng nghìn lượt
                if (existingShowtimes.Count >= 30) return;

                var newShowtimes = new List<Showtime>();

                // Nhóm các phòng theo từng cụm rạp
                var hallsByCinema = allHalls.GroupBy(h => h.CinemaId).ToList();

                // Lên lịch chiếu cho 3 ngày: Hôm nay, Ngày mai, Ngày kia
                for (int dayOffset = 0; dayOffset < 3; dayOffset++)
                {
                    var targetDate = startDate.AddDays(dayOffset);

                    foreach (var group in hallsByCinema)
                    {
                        var cinemaHalls = group.ToList();

                        for (int hIdx = 0; hIdx < cinemaHalls.Count; hIdx++)
                        {
                            var hall = cinemaHalls[hIdx];
                            var hallTypeUpper = (hall.HallType ?? "").ToUpper();

                            // Chọn danh sách phim và định dạng phù hợp một cách hợp lý với từng phòng:
                            // - Phòng IMAX Laser: Ưu tiên phim có định dạng IMAX
                            // - Phòng 3D Atmos: Ưu tiên phim có định dạng 3D hoặc âm thanh vòm Atmos
                            // - Phòng L'Amour / Gold Class: Giường nằm / Ghế VIP cao cấp
                            // - Phòng 4DX: Ghế chuyển động & hiệu ứng môi trường
                            // - Phòng Standard 2D: Chiếu phim 2D Digital
                            List<Movie> compatibleMovies;
                            string showtimeFormat;
                            decimal basePrice;

                            if (hallTypeUpper.Contains("IMAX"))
                            {
                                var imaxMovies = nowShowingMovies.Where(m => (m.Formats ?? "").ToUpper().Contains("IMAX")).ToList();
                                compatibleMovies = imaxMovies.Any() ? imaxMovies : nowShowingMovies;
                                showtimeFormat = "IMAX Laser";
                                basePrice = 160000;
                            }
                            else if (hallTypeUpper.Contains("3D") || hallTypeUpper.Contains("ATMOS"))
                            {
                                var d3Movies = nowShowingMovies.Where(m => (m.Formats ?? "").ToUpper().Contains("3D")).ToList();
                                compatibleMovies = d3Movies.Any() ? d3Movies : nowShowingMovies;
                                showtimeFormat = d3Movies.Any() ? "3D Atmos" : "2D Atmos";
                                basePrice = 125000;
                            }
                            else if (hallTypeUpper.Contains("L'AMOUR") || hallTypeUpper.Contains("LAMOUR"))
                            {
                                compatibleMovies = nowShowingMovies;
                                showtimeFormat = "L'Amour";
                                basePrice = 250000;
                            }
                            else if (hallTypeUpper.Contains("4DX"))
                            {
                                var f4dxMovies = nowShowingMovies.Where(m => (m.Formats ?? "").ToUpper().Contains("4DX")).ToList();
                                compatibleMovies = f4dxMovies.Any() ? f4dxMovies : nowShowingMovies;
                                showtimeFormat = "4DX";
                                basePrice = 180000;
                            }
                            else
                            {
                                // Standard 2D Digital
                                compatibleMovies = nowShowingMovies;
                                showtimeFormat = "2D DIGITAL";
                                basePrice = 105000;
                            }

                            // Giờ mở cửa chiếu đầu ngày (09:30, so le theo từng phòng)
                            var startHour = 9 + (hIdx % 2);
                            var startMinute = (hIdx * 25) % 60;
                            var cursorTime = targetDate.AddHours(startHour).AddMinutes(startMinute);

                            for (int round = 0; round < 5; round++)
                            {
                                var movie = compatibleMovies[(round + hIdx + dayOffset) % compatibleMovies.Count];
                                var startTime = cursorTime;
                                var endTime = startTime.AddMinutes(movie.Duration);

                                if (startTime.Hour >= 23 && startTime.Minute > 15) break;

                                // Kiểm tra trùng phòng trong DB và trong batch chuẩn bị thêm
                                bool conflictInDb = existingShowtimes.Any(s =>
                                    s.CinemaHallId == hall.Id && s.StartTime < endTime && s.EndTime > startTime);

                                bool conflictInBatch = newShowtimes.Any(s =>
                                    s.CinemaHallId == hall.Id && s.StartTime < endTime && s.EndTime > startTime);

                                if (!conflictInDb && !conflictInBatch)
                                {
                                    newShowtimes.Add(new Showtime
                                    {
                                        MovieId = movie.Id,
                                        CinemaHallId = hall.Id,
                                        StartTime = startTime,
                                        EndTime = endTime,
                                        Price = basePrice,
                                        Format = showtimeFormat
                                    });

                                    cursorTime = endTime.AddMinutes(15); // Nghỉ 15 phút dọn rạp
                                }
                                else
                                {
                                    cursorTime = cursorTime.AddMinutes(30);
                                }
                            }
                        }
                    }
                }

                if (newShowtimes.Any())
                {
                    await context.Showtimes.AddRangeAsync(newShowtimes);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore seed error
            }
        }

        public static async Task EnsureAdminActionLogsAndSampleBookingsAsync(ApplicationDbContext context)
        {
            try
            {
                // 1. Tạo bảng AdminActionLogs nếu chưa có
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AdminActionLogs')
                    BEGIN
                        CREATE TABLE [AdminActionLogs] (
                            [Id] int NOT NULL IDENTITY,
                            [ActionType] nvarchar(max) NOT NULL,
                            [ModuleName] nvarchar(max) NOT NULL,
                            [Description] nvarchar(max) NOT NULL,
                            [EntityId] nvarchar(max) NULL,
                            [PerformedBy] nvarchar(max) NOT NULL,
                            [IpAddress] nvarchar(max) NULL,
                            [CinemaId] int NULL,
                            [City] nvarchar(max) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                            [Status] nvarchar(max) NOT NULL DEFAULT N'Thành công',
                            CONSTRAINT [PK_AdminActionLogs] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_AdminActionLogs_Cinemas_CinemaId] FOREIGN KEY ([CinemaId]) REFERENCES [Cinemas] ([Id]) ON DELETE SET NULL
                        );
                    END
                ");

                // 2. Khởi tạo nhật ký thao tác mẫu nếu bảng đang trống
                if (!await context.AdminActionLogs.AnyAsync())
                {
                    var cinemas = await context.Cinemas.ToListAsync();
                    var cgvLandmark = cinemas.FirstOrDefault(c => c.Name.Contains("Landmark 81")) ?? cinemas.FirstOrDefault(c => c.City == "Hồ Chí Minh");
                    var cgvDongKhoi = cinemas.FirstOrDefault(c => c.Name.Contains("Đồng Khởi"));
                    var cgvBaTrieu = cinemas.FirstOrDefault(c => c.Name.Contains("Bà Triệu")) ?? cinemas.FirstOrDefault(c => c.City == "Hà Nội");
                    var cgvDaNang = cinemas.FirstOrDefault(c => c.City == "Đà Nẵng");
                    var cgvCanTho = cinemas.FirstOrDefault(c => c.City == "Cần Thơ");
                    var cgvHaiPhong = cinemas.FirstOrDefault(c => c.City == "Hải Phòng");
                    var cgvBinhDuong = cinemas.FirstOrDefault(c => c.City == "Bình Dương");

                    var now = DateTime.Now;
                    var logs = new List<AdminActionLog>
                    {
                        new AdminActionLog
                        {
                            ActionType = "Đăng nhập",
                            ModuleName = "Hệ thống",
                            Description = "Quản trị viên admin@cgv.vn đăng nhập thành công vào phiên làm việc quản trị",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            City = "Toàn quốc",
                            CreatedAt = now.AddMinutes(-15),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Đổi trạng thái",
                            ModuleName = "Đơn vé & Giao dịch",
                            Description = "Xác nhận duyệt thanh toán đơn vé CGV-20261026-8821 (Khách: Nguyễn Minh Tuấn, Tổng tiền: 240,000 đ)",
                            EntityId = "CGV-20261026-8821",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            CinemaId = cgvLandmark?.Id,
                            City = cgvLandmark?.City ?? "Hồ Chí Minh",
                            CreatedAt = now.AddMinutes(-40),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Thêm mới",
                            ModuleName = "Suất chiếu",
                            Description = "Tạo suất chiếu 2D phim 'Mai' lúc 19:30 ngày mai tại Phòng Cinema 01 - CGV Landmark 81",
                            EntityId = "ST-9912",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            CinemaId = cgvLandmark?.Id,
                            City = cgvLandmark?.City ?? "Hồ Chí Minh",
                            CreatedAt = now.AddHours(-1).AddMinutes(-12),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Cập nhật",
                            ModuleName = "Cụm rạp",
                            Description = "Cập nhật hotline và lịch bảo dưỡng định kỳ hệ thống âm thanh Dolby Atmos tại CGV Vincom Bà Triệu",
                            EntityId = cgvBaTrieu?.Id.ToString(),
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.25",
                            CinemaId = cgvBaTrieu?.Id,
                            City = cgvBaTrieu?.City ?? "Hà Nội",
                            CreatedAt = now.AddHours(-2).AddMinutes(-40),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Giao dịch vé",
                            ModuleName = "Đơn vé & Giao dịch",
                            Description = "Khách hàng Lê Thị Thảo hoàn tất giao dịch đặt 2 vé xem phim qua cổng VNPAY-QR tại rạp CGV Vincom Bà Triệu",
                            EntityId = "CGV-20261026-4432",
                            PerformedBy = "Cổng thanh toán VNPAY",
                            IpAddress = "113.161.44.89",
                            CinemaId = cgvBaTrieu?.Id,
                            City = cgvBaTrieu?.City ?? "Hà Nội",
                            CreatedAt = now.AddHours(-3).AddMinutes(-20),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Thêm mới",
                            ModuleName = "Suất chiếu",
                            Description = "Mở thêm 2 suất chiếu đặc biệt định dạng IMAX phục vụ khán giả cuối tuần tại CGV Vincom Đà Nẵng",
                            EntityId = "ST-9945",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            CinemaId = cgvDaNang?.Id,
                            City = cgvDaNang?.City ?? "Đà Nẵng",
                            CreatedAt = now.AddHours(-5),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Xử lý yêu cầu",
                            ModuleName = "Thuê rạp & Sự kiện",
                            Description = "Duyệt yêu cầu thuê phòng chiếu tổ chức hội thảo khách hàng tại CGV Vincom Đà Nẵng",
                            EntityId = "REQ-108",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            CinemaId = cgvDaNang?.Id,
                            City = cgvDaNang?.City ?? "Đà Nẵng",
                            CreatedAt = now.AddHours(-6).AddMinutes(-15),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Cập nhật",
                            ModuleName = "Bắp nước & F&B",
                            Description = "Cập nhật chương trình khuyến mãi giảm 20% Combo My Combo tại CGV Sense City Cần Thơ",
                            EntityId = "FB-02",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.15",
                            CinemaId = cgvCanTho?.Id,
                            City = cgvCanTho?.City ?? "Cần Thơ",
                            CreatedAt = now.AddHours(-8),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Đổi trạng thái",
                            ModuleName = "Đơn vé & Giao dịch",
                            Description = "Hủy đơn vé CGV-20261025-1102 theo yêu cầu hoàn hủy của khách hàng do bận công tác",
                            EntityId = "CGV-20261025-1102",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.15",
                            CinemaId = cgvCanTho?.Id,
                            City = cgvCanTho?.City ?? "Cần Thơ",
                            CreatedAt = now.AddHours(-10),
                            Status = "Cảnh báo"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Thêm mới",
                            ModuleName = "Tin tức & Ưu đãi",
                            Description = "Đăng bài viết ưu đãi mới 'Thứ Tư Vui Vẻ - Vé 2D chỉ từ 75.000đ' trên toàn hệ thống CGV",
                            EntityId = "NEWS-55",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            City = "Toàn quốc",
                            CreatedAt = now.AddDays(-1).AddHours(2),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Cập nhật",
                            ModuleName = "Phim",
                            Description = "Cập nhật trailer và poster chuẩn 4K cho danh sách phim sắp chiếu tháng tới",
                            EntityId = "MOV-12",
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.10",
                            City = "Toàn quốc",
                            CreatedAt = now.AddDays(-1).AddHours(-2),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Giao dịch vé",
                            ModuleName = "Đơn vé & Giao dịch",
                            Description = "Khách hàng Trần Hoàng Nam thanh toán MoMo thành công 4 vé xem phim tại CGV Vincom Hải Phòng",
                            EntityId = "CGV-20261025-7890",
                            PerformedBy = "Cổng thanh toán MoMo",
                            IpAddress = "27.72.102.15",
                            CinemaId = cgvHaiPhong?.Id,
                            City = cgvHaiPhong?.City ?? "Hải Phòng",
                            CreatedAt = now.AddDays(-1).AddHours(-4),
                            Status = "Thành công"
                        },
                        new AdminActionLog
                        {
                            ActionType = "Cập nhật",
                            ModuleName = "Cụm rạp",
                            Description = "Kiểm tra kỹ thuật màn chiếu IMAX Laser định kỳ tại CGV Aeon Mall Bình Dương Canary",
                            EntityId = cgvBinhDuong?.Id.ToString(),
                            PerformedBy = "admin@cgv.vn",
                            IpAddress = "192.168.1.30",
                            CinemaId = cgvBinhDuong?.Id,
                            City = cgvBinhDuong?.City ?? "Bình Dương",
                            CreatedAt = now.AddDays(-2),
                            Status = "Thành công"
                        }
                    };

                    await context.AdminActionLogs.AddRangeAsync(logs);
                    await context.SaveChangesAsync();
                }

                // 3. Khởi tạo thêm đơn đặt vé phong phú cho nhiều rạp và tỉnh thành khác nhau
                if (await context.Bookings.CountAsync() < 12)
                {
                    var showtimes = await context.Showtimes
                        .Include(s => s.Movie)
                        .Include(s => s.CinemaHall)
                            .ThenInclude(h => h!.Cinema)
                        .Take(30)
                        .ToListAsync();

                    if (showtimes.Any())
                    {
                        var foodItems = await context.FoodItems.Take(4).ToListAsync();
                        var sampleNames = new[] {
                            ("Phạm Đức Thắng", "thang.pham@gmail.com", "0912345678", "VNPAY-QR", "C4, C5", "Đã thanh toán"),
                            ("Hoàng Lan Anh", "lananh.hoang@yahoo.com", "0987654321", "MoMo", "D7, D8", "Đã thanh toán"),
                            ("Vũ Đình Khoa", "khoa.vu@hotmail.com", "0908877665", "ZaloPay", "E5, E6, E7", "Đã thanh toán"),
                            ("Đỗ Thanh Trúc", "thanhtruc.do@gmail.com", "0933221100", "Thẻ ATM/Visa", "F4, F5", "Đã thanh toán"),
                            ("Bùi Minh Khang", "khang.bui@gmail.com", "0977665544", "VNPAY-QR", "B3, B4", "Chờ xử lý"),
                            ("Nguyễn Thị Mai", "mai.nguyen@gmail.com", "0944112233", "MoMo", "G6, G7", "Đã thanh toán"),
                            ("Lê Quốc Bảo", "bao.le@outlook.com", "0918776655", "Tiền mặt", "C8, C9", "Đã thanh toán"),
                            ("Trần Thúy Nga", "thuynga.tran@gmail.com", "0903445566", "ZaloPay", "D1, D2", "Đã hủy"),
                            ("Phan Thanh Hùng", "hung.phan@gmail.com", "0988990011", "VNPAY-QR", "E8, E9", "Đã thanh toán"),
                            ("Võ Khánh Linh", "linh.vo@gmail.com", "0934567890", "Thẻ ATM/Visa", "F8, F9", "Đã thanh toán")
                        };

                        var random = new Random(42);
                        var now = DateTime.Now;
                        for (int i = 0; i < sampleNames.Length; i++)
                        {
                            var item = sampleNames[i];
                            var st = showtimes[i % showtimes.Count];
                            var seatCount = item.Item5.Split(',').Length;
                            var ticketTotal = st.Price * seatCount + (item.Item5.Contains("C") || item.Item5.Contains("D") ? 15000 * seatCount : 0);
                            
                            var hasFood = i % 2 == 0 && foodItems.Any();
                            var foodTotal = hasFood ? 115000m : 0m;
                            var totalAmount = ticketTotal + foodTotal;
                            var seatsList = hasFood ? $"{item.Item5} (+ Bắp nước: 1x CGV Combo)" : item.Item5;

                            var booking = new Booking
                            {
                                BookingCode = $"CGV-{now.AddHours(-i * 4):yyyyMMdd}-{random.Next(1000, 9999)}",
                                CustomerName = item.Item1,
                                CustomerEmail = item.Item2,
                                CustomerPhone = item.Item3,
                                ShowtimeId = st.Id,
                                BookingDate = now.AddHours(-i * 5).AddMinutes(-random.Next(10, 50)),
                                SeatsList = seatsList,
                                TotalAmount = totalAmount,
                                PaymentMethod = item.Item4,
                                Status = item.Item6
                            };

                            context.Bookings.Add(booking);
                            await context.SaveChangesAsync();

                            if (hasFood && foodItems.Any())
                            {
                                context.BookingFoodItems.Add(new BookingFoodItem
                                {
                                    BookingId = booking.Id,
                                    FoodItemId = foodItems[0].Id,
                                    Quantity = 1,
                                    UnitPrice = foodItems[0].Price
                                });
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore seed error
            }
        }

        public static async Task EnsureMemberTiersAndSeatHoldsTableAsync(ApplicationDbContext context, UserManager<ApplicationUser>? userManager)
        {
            try
            {
                // 1. Tạo bảng CAPBAC_THANHVIEN nếu chưa tồn tại
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CAPBAC_THANHVIEN')
                    BEGIN
                        CREATE TABLE [CAPBAC_THANHVIEN] (
                            [MaCapBac] int NOT NULL IDENTITY(1,1),
                            [TenCapBac] nvarchar(100) NOT NULL,
                            [DiemYeuCau] int NOT NULL DEFAULT 0,
                            [PhanTramGiamGia] decimal(5,2) NOT NULL DEFAULT 0,
                            [MoTa] nvarchar(max) NULL,
                            [MauHienThi] nvarchar(50) NOT NULL DEFAULT '#757575',
                            CONSTRAINT [PK_CAPBAC_THANHVIEN] PRIMARY KEY ([MaCapBac])
                        );
                    END
                ");

                // 2. Tạo bảng GIUCHO nếu chưa tồn tại
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GIUCHO')
                    BEGIN
                        CREATE TABLE [GIUCHO] (
                            [MaGiuCho] int NOT NULL IDENTITY(1,1),
                            [MaSuatChieu] int NOT NULL,
                            [MaGhe] nvarchar(20) NOT NULL,
                            [MaKH] nvarchar(450) NULL,
                            [ThoiGianHetHan] datetime2 NOT NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT [PK_GIUCHO] PRIMARY KEY ([MaGiuCho]),
                            CONSTRAINT [FK_GIUCHO_Showtimes_MaSuatChieu] FOREIGN KEY ([MaSuatChieu]) REFERENCES [Showtimes] ([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_GIUCHO_AspNetUsers_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                        );
                    END
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GIUCHO_MaSuatChieu_MaGhe')
                    BEGIN
                        CREATE INDEX [IX_GIUCHO_MaSuatChieu_MaGhe] ON [GIUCHO] ([MaSuatChieu], [MaGhe]);
                    END
                ");

                // 3. Thêm cột MemberTierId vào AspNetUsers nếu chưa có
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'MemberTierId')
                    BEGIN
                        ALTER TABLE [AspNetUsers] ADD [MemberTierId] int NULL;
                    END
                ");

                // 4. Seed dữ liệu mặc định cho CAPBAC_THANHVIEN
                if (!await context.MemberTiers.AnyAsync())
                {
                    var tiers = new List<MemberTier>
                    {
                        new MemberTier
                        {
                            TierName = "Thành viên Thường",
                            MinPoints = 0,
                            DiscountPercent = 0,
                            BadgeColor = "#607d8b",
                            Description = "Hạng tiêu chuẩn: Tích lũy 10% điểm thưởng cho mỗi giao dịch mua vé & bắp nước tại CGV."
                        },
                        new MemberTier
                        {
                            TierName = "Thành viên VIP",
                            MinPoints = 500,
                            DiscountPercent = 5,
                            BadgeColor = "#f59e0b",
                            Description = "Hạng VIP (500 điểm): Giảm ngay 5% giá vé khi mua trực tuyến, tặng 01 bắp rang sinh nhật & ưu tiên giữ chỗ trước."
                        },
                        new MemberTier
                        {
                            TierName = "Thành viên VVIP",
                            MinPoints = 1000,
                            DiscountPercent = 10,
                            BadgeColor = "#e11d48",
                            Description = "Hạng Thượng Hạng (1000 điểm): Giảm ngay 10% giá vé mọi suất chiếu, phòng chờ VIP sang trọng, nhận vé mời xem phim miễn phí định kỳ."
                        }
                    };

                    await context.MemberTiers.AddRangeAsync(tiers);
                    await context.SaveChangesAsync();
                }

                // 5. Cập nhật cấp bậc thành viên cho các User hiện tại dựa theo RewardPoints
                var allTiers = await context.MemberTiers.OrderByDescending(t => t.MinPoints).ToListAsync();
                var users = await context.Users.ToListAsync();
                bool usersUpdated = false;

                foreach (var u in users)
                {
                    var matchedTier = allTiers.FirstOrDefault(t => u.RewardPoints >= t.MinPoints);
                    if (matchedTier != null && (u.MemberTierId != matchedTier.Id || u.MemberTier != matchedTier.TierName))
                    {
                        u.MemberTierId = matchedTier.Id;
                        u.MemberTier = matchedTier.TierName;
                        usersUpdated = true;
                    }
                }

                if (usersUpdated)
                {
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore seed error
            }
        }

        private static async Task EnsureSoftDeleteAndDetailedRentalColumnsAsync(ApplicationDbContext context)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'AdminNotes')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [AdminNotes] nvarchar(max) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'EstimatedBudget')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [EstimatedBudget] decimal(18,2) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'TimeSlot')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [TimeSlot] nvarchar(100) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'MovieTitle')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [MovieTitle] nvarchar(255) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'BookingMode')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [BookingMode] nvarchar(100) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'IsHoldLocked')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [IsHoldLocked] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'HoldExpiresAt')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [HoldExpiresAt] datetime2 NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'AllocatedHalls')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [AllocatedHalls] nvarchar(255) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HallRentalRequests' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [HallRentalRequests] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContactInquiries' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [ContactInquiries] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Movies' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [Movies] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cinemas' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [Cinemas] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CinemaHalls' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [CinemaHalls] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CinemaHalls' AND COLUMN_NAME = 'SeatMatrixJson')
                    BEGIN
                        ALTER TABLE [CinemaHalls] ADD [SeatMatrixJson] nvarchar(max) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Showtimes' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [Showtimes] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FoodItems' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [FoodItems] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Bookings' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [Bookings] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NewsOffers' AND COLUMN_NAME = 'IsDeleted')
                    BEGIN
                        ALTER TABLE [NewsOffers] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                    END

                    -- Đảm bảo không có NULL hoặc lỗi font cho các cột mới thêm trong HallRentalRequests
                    UPDATE [HallRentalRequests] SET [BookingMode] = N'Bao trọn phòng (Khóa suất)' WHERE [BookingMode] IS NULL OR [BookingMode] LIKE N'%Tr%n%' OR [BookingMode] LIKE N'%?%';
                    UPDATE [HallRentalRequests] SET [TimeSlot] = N'Ca chiều (13:30 - 17:30)' WHERE [TimeSlot] IS NULL OR [TimeSlot] LIKE N'%chi%u%' OR [TimeSlot] LIKE N'%?%';
                    UPDATE [HallRentalRequests] SET [MovieTitle] = N'Tự chiếu nội bộ (Slide / Clip / Hội thảo)' WHERE [MovieTitle] IS NULL OR [MovieTitle] LIKE N'%chi%u%' OR [MovieTitle] LIKE N'%?%';
                    UPDATE [HallRentalRequests] SET [AllocatedHalls] = N'' WHERE [AllocatedHalls] IS NULL;
                    UPDATE [HallRentalRequests] SET [IsHoldLocked] = 0 WHERE [IsHoldLocked] IS NULL;
                    UPDATE [HallRentalRequests] SET [IsDeleted] = 0 WHERE [IsDeleted] IS NULL;
                ");
            }
            catch
            {
                // Ignore if running on another provider or columns already exist
            }
        }
    }
}
