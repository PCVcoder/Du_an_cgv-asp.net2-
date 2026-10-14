// models.js - File dữ liệu tĩnh (Mock Data) hỗ trợ cho các trang HTML
// Nhúng file này vào các trang HTML bằng thẻ: <script src="models.js"></script>

/* =========================================
   1. DỮ LIỆU RẠP CHIẾU (Cinemas)
========================================= */
const cities = [
    {
      name: "Hồ Chí Minh",
      cinemas: [
        { name: "CGV Hùng Vương Plaza" }, { name: "CGV Vivo City" }, { name: "CGV Menas Mall (CGV CT Plaza)" },
        { name: "CGV Hoàng Văn Thụ" }, { name: "CGV Vincom Center Landmark 81" }, { name: "CGV Vincom Mega Mall Grand Park" },
        { name: "CGV Crescent Mall" }, { name: "CGV Pearl Plaza" }, { name: "CGV Pandora City" },
        { name: "CGV Aeon Bình Tân" }, { name: "CGV Satra Củ Chi" }, { name: "CGV Thảo Điền Pearl" },
        { name: "CGV Liberty Citypoint" }, { name: "CGV Aeon Tân Phú" }, { name: "CGV Saigonres Nguyễn Xí" },
        { name: "CGV Gigamall Thủ Đức" }, { name: "CGV Vincom Thủ Đức" }, { name: "CGV Vincom Đồng Khởi" },
        { name: "CGV Vincom Gò Vấp" }, { name: "CGV Sư Vạn Hạnh" }, { name: "CGV Lý Chính Thắng" }
      ],
    },
    {
      name: "Hà Nội",
      cinemas: [
        { name: "CGV Vincom Bà Triệu" }, { name: "CGV Vincom Royal City" }, { name: "CGV Vincom Times City" },
        { name: "CGV Aeon Mall Long Biên" }, { name: "CGV Vincom Trần Duy Hưng" }, { name: "CGV Vincom Nguyễn Chí Thanh" },
        { name: "CGV Lotte Center Hà Nội" }
      ],
    },
    { name: "Đà Nẵng", cinemas: [{ name: "CGV Vincom Đà Nẵng" }, { name: "CGV Lotte Mart Đà Nẵng" }, { name: "CGV Đà Nẵng Center" }] },
    { name: "Cần Thơ", cinemas: [{ name: "CGV Sense City Cần Thơ" }, { name: "CGV Vincom Cần Thơ" }] },
    { name: "Đồng Nai", cinemas: [{ name: "CGV Vincom Biên Hòa" }] },
    { name: "Bình Dương", cinemas: [{ name: "CGV Aeon Mall Bình Dương" }, { name: "CGV Vincom Bình Dương" }] },
    { name: "Hải Phòng", cinemas: [{ name: "CGV Vincom Hải Phòng" }, { name: "CGV Lotte Mart Hải Phòng" }] },
    { name: "Khánh Hòa", cinemas: [{ name: "CGV Nha Trang Center" }, { name: "CGV Lotte Mart Nha Trang" }] }
    // ... (Có thể bổ sung thêm các tỉnh thành khác)
  ];
  
  
/* =========================================
   2. DỮ LIỆU PHIM (Movies)
========================================= */
const imgUrl = (id) => `https://images.unsplash.com/${id}?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&w=400`;
  
const nowShowingMovies = [
    { id: 1, title: "Lật Mặt 7: Một Điều Ước", rating: "T16", ratingColor: "#2e7d32", genre: ["Gia đình", "Tâm lý"], duration: 121, endDate: "18-09-2026", img: imgUrl("photo-1568038479111-87bf80659645"), rank: 1 },
    { id: 2, title: "Vùng Bắt Quỷ Dữ", rating: "T18", ratingColor: "#b71c1c", genre: ["Hành động", "Kinh dị"], duration: 94, endDate: "18-09-2026", img: imgUrl("photo-1778973812223-e5079431fa47"), special: "THE NEAK SHOW" },
    { id: 3, title: "Tế Vật Cải Mệnh", rating: "T18", ratingColor: "#b71c1c", genre: ["Kinh dị"], duration: 103, endDate: "18-09-2026", img: imgUrl("photo-1651627567991-e4ec7b8fc72c"), special: "THE NEAK SHOW" },
    { id: 4, title: "Kỳ Nghỉ Hè Nhớ Đời", rating: "T13", ratingColor: "#e65100", genre: ["Gia đình", "Hài", "Tâm lý"], duration: 113, endDate: "21-09-2026", img: imgUrl("photo-1609062757924-6c2d01b3b422") },
    { id: 5, title: "Hope: Vùng Tử Địa", rating: "T16", ratingColor: "#2e7d32", genre: ["Gia đình", "Hài"], duration: 137, endDate: "04-09-2026", img: imgUrl("photo-1634733049839-0292be607569"), rank: 2, special: "IMAX" },
    { id: 6, title: "Quý Tử Vượt Giàu Bần Đói Khổ", rating: "K", ratingColor: "#1565c0", genre: ["Hoạt hình", "Phiêu lưu"], duration: 110, endDate: "25-08-2026", img: imgUrl("photo-1581833971358-2c8b550f87b3"), rank: 3 },
    { id: 7, title: "Mắc Nợ Một Lời Tạm Biệt", rating: "T18", ratingColor: "#b71c1c", genre: ["Tâm lý"], duration: 103, endDate: "11-09-2026", img: imgUrl("photo-1532378193110-4c1209251872") },
    { id: 8, title: "Có Thật Sự Ngại", rating: "T16", ratingColor: "#2e7d32", genre: ["Tâm lý"], duration: 86, endDate: "11-09-2026", img: imgUrl("photo-1563170446-9c3c0622d8a9") },
    { id: 9, title: "Bùa Yêu: Bí Mật Gia Tộc", rating: "T13", ratingColor: "#e65100", genre: ["Thần thoại", "Tình cảm"], duration: 130, endDate: "11-09-2026", img: imgUrl("photo-1653395108060-e0236d7a9e58") },
    { id: 10, title: "Chikawa: Bí Mật Đảo Người Cá", rating: "K", ratingColor: "#1565c0", genre: ["Hoạt hình"], duration: 99, endDate: "28-08-2026", img: imgUrl("photo-1581436670376-f152b9e3b5ec") }
];
  
const comingSoonMovies = [
    { id: 101, title: "Marnie Yêu Dấu", rating: "K", ratingColor: "#1565c0", genre: ["Hoạt hình"], duration: 103, endDate: "19-10-2026", img: imgUrl("photo-1563170446-9c3c0622d8a9") },
    { id: 102, title: "Bông Hoa Nhà Hát", rating: "T16", ratingColor: "#2e7d32", genre: ["Kinh dị"], duration: 97, endDate: "19-10-2026", img: imgUrl("photo-1609062757924-6c2d01b3b422") },
    { id: 103, title: "Yêu Nhân Thần Ký Trường An", rating: "K", ratingColor: "#1565c0", genre: ["Hoạt hình", "Hài hộp"], duration: 118, endDate: "19-10-2026", img: imgUrl("photo-1581833971358-2c8b550f87b3") },
    { id: 104, title: "Avengers: Hội Kết - Phiên Bản Đặc Biệt", rating: "T13", ratingColor: "#e65100", genre: ["Hành động", "Khoa học viễn tưởng"], duration: 181, endDate: "19-10-2026", img: imgUrl("photo-1754677315657-b6c9cf2650ef"), special: "IMAX" }
];
  

/* =========================================
   3. DỮ LIỆU TIN TỨC VÀ ƯU ĐÃI (News & Offers)
========================================= */
const newsOffers = [
    {
      id: "double-deal",
      title: "CGVXCOCA-COLA: DOUBLE DEAL – NHÂN ĐÔI BẮP NƯỚC",
      category: "offers",
      categoryLabel: "ƯU ĐÃI",
      dateRange: "18/09/2026 - 01/10/2026",
      image: "https://images.unsplash.com/photo-1568702846914-96b305d2aaeb?w=600&q=80",
      summary: "Chỉ 249K mà có tới 5 món và tặng thêm bắp nước?! Mua DOUBLE DEAL COMBO ngay hôm nay!"
    },
    {
      id: "tang-poster",
      title: "SƯU TẬP 5 MẢNH KÝ ỨC ENDGAME – TẶNG 1 POSTER",
      category: "news",
      categoryLabel: "TIN MỚI",
      dateRange: "24/09/2026 - 02/10/2026",
      image: "https://images.unsplash.com/photo-1635805737707-575885ab0820?w=600&q=80",
      summary: "Sưu tập 5 mảnh ký ức Avengers Endgame và nhận ngay 1 Poster siêu to khổng lồ – chỉ có tại CGV!"
    },
    {
      id: "member-day-coupon",
      title: "CGV MEMBER DAY – COUPON GIẢM 99.000Đ",
      category: "member",
      categoryLabel: "THÀNH VIÊN",
      dateRange: "CGV Member Day",
      image: "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=600&q=80",
      summary: "X2 điểm thưởng và nhận Coupon giảm 99.000đ dành riêng cho thành viên CGV trong ngày Member Day."
    },
    {
      id: "banh-trang-tron",
      title: "BÁNH TRÁNG TRỘN – SẢN PHẨM MỚI CHỈ 25.000Đ",
      category: "news",
      categoryLabel: "TIN MỚI",
      dateRange: "28/8/2026",
      image: "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600&q=80",
      summary: "Cine De Chef ra mắt sản phẩm mới – Bánh Tráng Trộn với nhiều vị: Rong Biển, Mala, Tê Cay chỉ 25.000đ."
    }
];

// Hàm để lấy thông tin chi tiết phim ngẫu nhiên (nếu cần)
const getMovieDetails = (movieTitle) => {
    return {
        cast: ["Trần Nghĩa", "Kaity Nguyễn", "Quốc Anh"],
        director: "Victor Vũ",
        language: "Tiếng Việt",
        synopsis: `${movieTitle} là một hành trình nhiều thử thách và những lựa chọn bất ngờ. Bộ phim mang đến cảm xúc gần gũi, các tình huống giàu kịch tính và thông điệp ý nghĩa dành cho khán giả.`
    }
};
