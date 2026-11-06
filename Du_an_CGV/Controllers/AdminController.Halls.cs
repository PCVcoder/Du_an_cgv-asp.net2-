using Du_an_CGV.Data;
using Du_an_CGV.Models;
using Du_an_CGV.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_an_CGV.Controllers
{
    public partial class AdminController
    {
        #region ===================== QUẢN LÝ PHÒNG CHIẾU (CINEMA HALLS) =====================

        [HttpGet]
        public async Task<IActionResult> GetCinemaHalls(int cinemaId)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null) return NotFound();

            var halls = await _context.CinemaHalls
                .Where(h => h.CinemaId == cinemaId)
                .OrderBy(h => h.Name)
                .Select(h => new
                {
                    h.Id,
                    h.CinemaId,
                    h.Name,
                    h.HallType,
                    h.TotalSeats,
                    h.IsDeleted,
                    SeatsCount = h.Seats.Count,
                    ShowtimesCount = h.Showtimes.Count
                })
                .ToListAsync();

            return Json(new
            {
                cinemaId = cinema.Id,
                cinemaName = cinema.Name,
                halls
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCinemaHall(int id)
        {
            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (hall == null) return NotFound();

            return Json(new
            {
                hall.Id,
                hall.CinemaId,
                CinemaName = hall.Cinema?.Name,
                hall.Name,
                hall.HallType,
                hall.TotalSeats
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCinemaHall(int cinemaId, string name, string hallType, int totalSeats = 60)
        {
            var cinema = await _context.Cinemas.FindAsync(cinemaId);
            if (cinema == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cụm rạp!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                var count = await _context.CinemaHalls.CountAsync(h => h.CinemaId == cinemaId);
                name = $"Cinema {count + 1:D2}";
            }

            var hall = new CinemaHall
            {
                CinemaId = cinemaId,
                Name = name,
                HallType = string.IsNullOrWhiteSpace(hallType) ? "Standard 2D" : hallType,
                TotalSeats = totalSeats > 0 ? totalSeats : 60
            };

            _context.CinemaHalls.Add(hall);
            await _context.SaveChangesAsync();

            // Auto generate seats for this hall
            int rowCount = (totalSeats <= 24) ? 3 : 5;
            int seatsPerRow = (totalSeats <= 24) ? 8 : 12;
            if (totalSeats >= 96)
            {
                rowCount = 8;
                seatsPerRow = 12;
            }
            await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount, seatsPerRow);

            TempData["SuccessMessage"] = $"Đã thêm phòng chiếu \"{hall.Name}\" ({hall.HallType}) cho rạp \"{cinema.Name}\"!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCinemaHall(int id, string name, string hallType, int totalSeats)
        {
            var hall = await _context.CinemaHalls.FindAsync(id);
            if (hall == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng chiếu!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            hall.Name = name;
            hall.HallType = hallType;
            if (totalSeats > 0 && totalSeats != hall.TotalSeats)
            {
                var now = DateTime.Now;
                var hasActive = await _context.Showtimes.AnyAsync(s => s.CinemaHallId == id && !s.IsDeleted && s.EndTime >= now);
                if (hasActive)
                {
                    TempData["ErrorMessage"] = "⚠️ KHÔNG THỂ ĐỔI SỨC CHỨA: Phòng chiếu này đang có suất chiếu đang mở bán hoặc chưa kết thúc! Quy tắc nghiệp vụ cấm sửa đổi sơ đồ ghế.";
                    return RedirectToAction("Index", new { tab = "cinemas" });
                }

                hall.TotalSeats = totalSeats;
                int rowCount = (totalSeats <= 24) ? 3 : 5;
                int seatsPerRow = (totalSeats <= 24) ? 8 : 12;
                await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount, seatsPerRow);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật phòng chiếu \"{hall.Name}\"!";
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCinemaHall(int id)
        {
            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .Include(h => h.Showtimes)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hall != null)
            {
                var cinemaName = hall.Cinema?.Name ?? "CGV";
                var hallName = hall.Name;
                hall.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã ẩn phòng chiếu \"{hallName}\" của rạp \"{cinemaName}\"! Dữ liệu được bảo toàn.";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreCinemaHall(int id)
        {
            var hall = await _context.CinemaHalls.Include(h => h.Cinema).FirstOrDefaultAsync(h => h.Id == id);
            if (hall != null)
            {
                hall.IsDeleted = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã khôi phục phòng chiếu \"{hall.Name}\" của rạp \"{hall.Cinema?.Name}\"!";
            }
            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdjustHallCount(int cinemaId, int targetCount)
        {
            var cinema = await _context.Cinemas
                .Include(c => c.CinemaHalls)
                .FirstOrDefaultAsync(c => c.Id == cinemaId);

            if (cinema == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cụm rạp!";
                return RedirectToAction("Index", new { tab = "cinemas" });
            }

            if (targetCount < 1) targetCount = 1;
            if (targetCount > 15) targetCount = 15;

            int currentCount = cinema.CinemaHalls.Count;
            if (targetCount > currentCount)
            {
                for (int i = currentCount + 1; i <= targetCount; i++)
                {
                    string hallType = i switch
                    {
                        1 => "Standard 2D",
                        2 => "3D Atmos",
                        3 => "IMAX Laser",
                        4 => "L'Amour Giường Nằm",
                        5 => "4DX",
                        6 => "Gold Class",
                        _ => "Standard 2D"
                    };
                    int seats = (hallType == "L'Amour Giường Nằm") ? 24 : 60;
                    var hall = new CinemaHall
                    {
                        CinemaId = cinema.Id,
                        Name = $"Cinema {i:D2}",
                        HallType = hallType,
                        TotalSeats = seats
                    };
                    _context.CinemaHalls.Add(hall);
                    await _context.SaveChangesAsync();
                    await DbInitializer.GenerateSeatsForHallAsync(_context, hall.Id, rowCount: 5, seatsPerRow: 12);
                }
                TempData["SuccessMessage"] = $"Đã tăng số phòng rạp \"{cinema.Name}\" từ {currentCount} lên {targetCount} phòng!";
            }
            else if (targetCount < currentCount)
            {
                var hallsToRemove = cinema.CinemaHalls
                    .OrderByDescending(h => h.Id)
                    .Take(currentCount - targetCount)
                    .ToList();

                _context.CinemaHalls.RemoveRange(hallsToRemove);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã điều chỉnh rạp \"{cinema.Name}\" xuống {targetCount} phòng chiếu!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Số phòng chiếu của rạp \"{cinema.Name}\" đã là {targetCount}!";
            }

            return RedirectToAction("Index", new { tab = "cinemas" });
        }

        [HttpGet]
        public async Task<IActionResult> GetHallSeatMatrix(int hallId)
        {
            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == hallId);

            if (hall == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phòng chiếu!" });
            }

            var now = DateTime.Now;
            // Quy tắc nghiệp vụ: Kiểm tra xem phòng chiếu có suất chiếu đang mở bán hoặc chưa kết thúc hay không
            var activeShowtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Where(s => s.CinemaHallId == hallId && !s.IsDeleted && s.EndTime >= now)
                .OrderBy(s => s.StartTime)
                .Take(10)
                .Select(s => new
                {
                    s.Id,
                    movieTitle = s.Movie != null ? s.Movie.Title : "Phim chưa đặt tên",
                    startTime = s.StartTime.ToString("dd/MM/yyyy HH:mm"),
                    endTime = s.EndTime.ToString("HH:mm")
                })
                .ToListAsync();

            bool hasActiveShowtimes = activeShowtimes.Any();

            var existingSeats = await _context.Seats
                .Where(s => s.CinemaHallId == hallId)
                .OrderBy(s => s.Row)
                .ThenBy(s => s.Number)
                .Select(s => new
                {
                    s.Row,
                    s.Number,
                    s.SeatCode,
                    s.SeatType,
                    s.PriceModifier
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                hallId = hall.Id,
                hallName = hall.Name,
                cinemaName = hall.Cinema?.Name ?? "CGV Cinemas",
                hallType = hall.HallType,
                totalSeats = hall.TotalSeats,
                savedMatrixJson = hall.SeatMatrixJson,
                canEdit = !hasActiveShowtimes,
                hasActiveShowtimes,
                activeShowtimesCount = activeShowtimes.Count,
                activeShowtimes,
                lockReason = hasActiveShowtimes 
                    ? $"⚠️ QUY TẮC BẢO VỆ: Phòng chiếu này hiện có {activeShowtimes.Count} suất chiếu đang mở bán hoặc sắp chiếu. Để đảm bảo an toàn cho các vé đã đặt, sơ đồ ghế hiện đang ở chế độ CHỈ XEM (không thể chỉnh sửa)!"
                    : "",
                existingSeats
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveHallSeatMatrix([FromBody] SaveSeatMatrixDto request)
        {
            if (request == null || request.HallId <= 0)
            {
                return Json(new { success = false, message = "⚠️ Dữ liệu cấu hình không hợp lệ!" });
            }

            var hall = await _context.CinemaHalls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == request.HallId);

            if (hall == null)
            {
                return Json(new { success = false, message = "⚠️ Không tìm thấy phòng chiếu cần cấu hình sơ đồ ghế!" });
            }

            // QUY TẮC NGHIỆP VỤ: Sơ đồ phòng chiếu đã có suất chiếu đang mở bán sẽ không được sửa
            var now = DateTime.Now;
            var hasActiveShowtimes = await _context.Showtimes
                .AnyAsync(s => s.CinemaHallId == request.HallId && !s.IsDeleted && s.EndTime >= now);

            if (hasActiveShowtimes)
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ KHÔNG THỂ SỬA ĐỔI: Phòng chiếu này đang có suất chiếu đang mở bán hoặc sắp chiếu! Theo quy tắc nghiệp vụ rạp phim, không được phép thay đổi sơ đồ ghế để bảo vệ tính toàn vẹn của các vé đã đặt."
                });
            }

            // LUỒNG NGOẠI LỆ 4A: Ma trận ghế lỗi hoặc không có lối đi
            if (request.Rows < 2 || request.Rows > 15 || request.Cols < 3 || request.Cols > 24)
            {
                return Json(new
                {
                    success = false,
                    message = $"⚠️ KÍCH THƯỚC MA TRẬN KHÔNG HỢP LỆ: Số hàng phải từ 2 đến 15, số cột phải từ 3 đến 24! (Hiện tại: {request.Rows} hàng x {request.Cols} cột)"
                });
            }

            if (request.Grid == null || request.Grid.Count != request.Rows)
            {
                return Json(new { success = false, message = "⚠️ Dữ liệu ma trận hàng không khớp với kích thước cấu hình!" });
            }

            int standardCount = 0;
            int vipCount = 0;
            int aisleCount = 0;

            for (int r = 0; r < request.Rows; r++)
            {
                if (request.Grid[r] == null || request.Grid[r].Count != request.Cols)
                {
                    return Json(new { success = false, message = $"⚠️ Hàng thứ {r + 1} thiếu số cột theo quy định!" });
                }

                for (int c = 0; c < request.Cols; c++)
                {
                    var cellType = request.Grid[r][c]?.Trim();
                    if (cellType == "Aisle" || cellType == "None")
                    {
                        aisleCount++;
                    }
                    else if (cellType == "VIP")
                    {
                        vipCount++;
                    }
                    else
                    {
                        standardCount++;
                    }
                }
            }

            // Luồng 4a: Không có lối đi
            if (aisleCount == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ MA TRẬN KHÔNG CÓ LỐI ĐI: Sơ đồ phòng chiếu bắt buộc phải có ít nhất 1 lối đi (ô lối đi) để đảm bảo an toàn thoát hiểm và luồng di chuyển của khán giả. Vui lòng tinh chỉnh lại sơ đồ!"
                });
            }

            int totalActualSeats = standardCount + vipCount;
            if (totalActualSeats == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ SƠ ĐỒ TRỐNG: Phòng chiếu phải có ít nhất một ghế ngồi (Thường hoặc VIP)!"
                });
            }

            // Xóa danh sách ghế cũ của phòng chiếu này
            var oldSeats = await _context.Seats.Where(s => s.CinemaHallId == hall.Id).ToListAsync();
            _context.Seats.RemoveRange(oldSeats);

            // Sinh mới danh sách ghế theo đúng ma trận
            var newSeats = new List<Seat>();
            for (int r = 0; r < request.Rows; r++)
            {
                string rowLabel = ((char)('A' + r)).ToString();
                int seatNumInRow = 1;

                for (int c = 0; c < request.Cols; c++)
                {
                    var cellType = request.Grid[r][c]?.Trim();
                    if (cellType == "Aisle" || cellType == "None")
                    {
                        // Là lối đi, không tạo bản ghi ghế
                        continue;
                    }

                    bool isVip = (cellType == "VIP");
                    newSeats.Add(new Seat
                    {
                        CinemaHallId = hall.Id,
                        Row = rowLabel,
                        Number = seatNumInRow,
                        SeatCode = $"{rowLabel}{seatNumInRow}",
                        SeatType = isVip ? "VIP" : "Standard",
                        PriceModifier = isVip ? 15000 : 0
                    });
                    seatNumInRow++;
                }
            }

            _context.Seats.AddRange(newSeats);

            // Cập nhật lại số ghế thực tế và lưu chuỗi JSON ma trận
            hall.TotalSeats = newSeats.Count;
            hall.SeatMatrixJson = System.Text.Json.JsonSerializer.Serialize(request);

            await _context.SaveChangesAsync();

            await LogActionAsync(
                "Cấu hình sơ đồ ghế",
                "Phòng chiếu",
                $"Cấu hình ma trận sơ đồ ghế phòng {hall.Name} ({hall.Cinema?.Name ?? "CGV"}): {request.Rows} hàng x {request.Cols} cột, {newSeats.Count} ghế ({standardCount} Thường, {vipCount} VIP, {aisleCount} lối đi)",
                hall.Id.ToString(),
                hall.CinemaId
            );

            return Json(new
            {
                success = true,
                message = $"✅ Đã lưu sơ đồ phòng chiếu thành công! Tổng cộng {newSeats.Count} ghế ({standardCount} ghế thường, {vipCount} ghế VIP) và {aisleCount} ô lối đi.",
                totalSeats = newSeats.Count,
                standardCount,
                vipCount,
                aisleCount
            });
        }

        #endregion
    }
}

