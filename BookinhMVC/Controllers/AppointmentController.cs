using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Models;
using BookinhMVC.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace BookinhMVC.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly BookingContext _context;
        private readonly IHubContext<BookingHub> _hubContext;

        public AppointmentController(BookingContext context, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // =============================================================
        // PHẦN 1: WEB MVC (Xử lý giao diện trình duyệt)
        // =============================================================

        // 1.1 Hiển thị trang đặt lịch (GET)
        [HttpGet]
        public async Task<IActionResult> Book(int? selectedDoctorId = null)
        {
            // Lấy danh sách bác sĩ
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.SelectedDoctorId = selectedDoctorId ?? 0;

            // --- LẤY THÔNG TIN BỆNH NHÂN TỪ DB ĐỂ ĐIỀN EMAIL/SĐT ---
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                // Sử dụng DbSet BenhNhans (hoặc TaiKhoanBenhNhan tùy vào BookingContext của bạn)
                // Dựa vào model bạn cung cấp, tên bảng thường là BenhNhans
                var patient = await _context.BenhNhans.FirstOrDefaultAsync(p => p.MaBenhNhan == userId.Value);
                if (patient != null)
                {
                    ViewBag.PatientPhone = patient.SoDienThoai;
                    ViewBag.PatientEmail = patient.Email;
                }
            }
            // -------------------------------------------------------------

            return View();
        }

        // 1.2 Xử lý Form đặt lịch từ Web (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int selectedDoctorId, DateTime selectedDate, TimeSpan selectedTime, string symptoms)
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            // 2. Ghép ngày + giờ
            DateTime finalDateTime = selectedDate.Date + selectedTime;

            // 3. Kiểm tra trùng lịch (Concurrency Check)
            bool isTaken = await _context.LichHens.AnyAsync(l => l.MaBacSi == selectedDoctorId && l.NgayGio == finalDateTime && l.TrangThai != "Đã hủy");

            if (isTaken)
            {
                ViewBag.Message = "Rất tiếc, khung giờ này vừa có người khác đặt. Vui lòng chọn giờ khác.";
                // Load lại dữ liệu bác sĩ để hiển thị lại View
                return await Book(selectedDoctorId);
            }

            // 4. Lưu vào Database
            var appt = new LichHen
            {
                MaBenhNhan = userId.Value,
                MaBacSi = selectedDoctorId,
                NgayGio = finalDateTime,
                TrieuChung = symptoms,
                TrangThai = "Chờ xác nhận",
                NgayTao = DateTime.Now,
                DaThongBao = false
            };

            _context.LichHens.Add(appt);
            await _context.SaveChangesAsync();

            // 5. Gửi SignalR thông báo cho Bác sĩ (Giống Mobile App)
            var patientName = HttpContext.Session.GetString("PatientName") ?? "Khách hàng Web";
            await _hubContext.Clients.Group("Doctors").SendAsync("ReceiveNewBooking", new
            {
                maLich = appt.MaLich,
                tenBenhNhan = patientName,
                ngayGio = appt.NgayGio,
                noiDung = $"WEB: Bệnh nhân {patientName} vừa đặt lịch lúc {appt.NgayGio:HH:mm dd/MM}!"
            });

            ViewBag.Message = "Đặt lịch thành công! Vui lòng chờ xác nhận.";
            return await Book(selectedDoctorId);
        }

        // 1.3 Xử lý đánh giá bác sĩ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int doctorId, int diemDanhGia, string nhanXet)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "User");

            var review = new DanhGia
            {
                MaBacSi = doctorId,
                MaBenhNhan = userId.Value,
                DiemDanhGia = diemDanhGia,
                NhanXet = nhanXet,
                NgayDanhGia = DateTime.Now
            };

            _context.DanhGias.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewMessage"] = "Cảm ơn bạn đã gửi đánh giá!";
            return RedirectToAction("Book", new { selectedDoctorId = doctorId });
        }


        // =============================================================
        // PHẦN 2: SHARED DATA & API (Dùng chung cho cả Web JS & Mobile)
        // =============================================================

        // API Lấy ngày rảnh (Web JQuery và Flutter đều gọi cái này)
        [HttpGet]
        public async Task<JsonResult> GetAvailableDates(int doctorId)
        {
            var dates = await _context.LichLamViecs
                .Where(l => l.MaBacSi == doctorId && l.TrangThai == "Đã xác nhận" && l.NgayLamViec >= DateTime.Today)
                .Select(l => l.NgayLamViec.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")).ToList());
        }

        // API Lấy giờ rảnh (Web JQuery và Flutter đều gọi cái này)
        [HttpGet]
        public async Task<JsonResult> GetAvailableTimes(int doctorId, string date)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime selectedDate))
                return Json(new { times = new List<string>() });

            // 1. Lấy ca làm việc
            var workSchedules = await _context.LichLamViecs
                .Where(lv => lv.MaBacSi == doctorId && lv.NgayLamViec.Date == selectedDate.Date && lv.TrangThai == "Đã xác nhận")
                .ToListAsync();

            // 2. Lấy giờ đã bị đặt
            var bookedTimes = await _context.LichHens
                .Where(l => l.MaBacSi == doctorId && l.NgayGio.Date == selectedDate.Date && l.TrangThai != "Đã hủy")
                .Select(l => l.NgayGio.TimeOfDay)
                .ToListAsync();

            var availableTimes = new List<string>();

            // 3. Tính toán Slot trống
            foreach (var schedule in workSchedules)
            {
                for (var t = schedule.GioBatDau; t < schedule.GioKetThuc; t = t.Add(TimeSpan.FromMinutes(30)))
                {
                    // Không lấy quá khứ nếu là hôm nay
                    if (selectedDate.Date == DateTime.Today && t <= DateTime.Now.TimeOfDay) continue;

                    // Kiểm tra xem giờ này có bị trùng với bookedTimes không
                    if (!bookedTimes.Any(bt => Math.Abs((bt - t).TotalMinutes) < 1))
                    {
                        availableTimes.Add(t.ToString(@"hh\:mm"));
                    }
                }
            }

            return Json(new { times = availableTimes });
        }


        // =============================================================
        // PHẦN 3: API RIÊNG CHO MOBILE APP
        // =============================================================

        // API Đặt lịch từ Mobile
        [HttpPost]
        [Route("api/Appointment/Book")]
        public async Task<IActionResult> BookApi([FromBody] BookAppointmentRequest request)
        {
            Console.WriteLine("📲 [API] Mobile gọi BookApi...");
            if (request == null) return BadRequest(new { message = "Dữ liệu trống" });

            // Mobile cần cơ chế Auth riêng (Token), ở đây tạm dùng Session giả định
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Vui lòng đăng nhập lại." });

            if (!int.TryParse(request.SelectedDoctorId, out int doctorId)) return BadRequest(new { message = "Lỗi ID bác sĩ" });
            if (!DateTime.TryParse(request.SelectedDate, out DateTime date)) return BadRequest(new { message = "Lỗi ngày" });
            if (!TimeSpan.TryParse(request.SelectedTime, out TimeSpan time)) return BadRequest(new { message = "Lỗi giờ" });

            var apptDate = date.Date.Add(time);

            // Check trùng
            bool exists = await _context.LichHens.AnyAsync(l => l.MaBacSi == doctorId && l.NgayGio == apptDate && l.TrangThai != "Đã hủy");
            if (exists) return BadRequest(new { message = "Giờ này đã có người đặt." });

            var appt = new LichHen
            {
                MaBenhNhan = userId.Value,
                MaBacSi = doctorId,
                NgayGio = apptDate,
                TrieuChung = request.Symptoms ?? "Đặt từ Mobile App",
                TrangThai = "Chờ xác nhận",
                NgayTao = DateTime.Now,
                DaThongBao = false
            };

            try
            {
                _context.LichHens.Add(appt);
                await _context.SaveChangesAsync();

                var patient = await _context.BenhNhans.FindAsync(userId.Value);
                var patientName = patient?.HoTen ?? "Khách Mobile";

                // SignalR Notification
                await _hubContext.Clients.Group("Doctors").SendAsync("ReceiveNewBooking", new
                {
                    maLich = appt.MaLich,
                    tenBenhNhan = patientName,
                    ngayGio = appt.NgayGio,
                    noiDung = $"MOBILE: Bệnh nhân {patientName} đặt lịch lúc {appt.NgayGio:HH:mm dd/MM}!"
                });

                return Ok(new { success = true, message = "Đặt lịch thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        // API Hủy lịch từ Mobile
        [HttpPost]
        [Route("api/Appointment/Cancel")]
        public async Task<IActionResult> CancelApi([FromBody] CancelRequest request)
        {
            try
            {
                var appt = await _context.LichHens.Include(l => l.BenhNhan).FirstOrDefaultAsync(l => l.MaLich == request.Id);
                if (appt == null) return NotFound(new { message = "Không tìm thấy lịch hẹn" });

                appt.TrangThai = "Đã hủy";

                var notif = new ThongBao
                {
                    MaNguoiDung = appt.MaBenhNhan,
                    TieuDe = "Đã hủy lịch",
                    NoiDung = $"Lịch hẹn #{appt.MaLich} đã hủy thành công.",
                    NgayTao = DateTime.Now,
                    MaLichHen = appt.MaLich,
                    DaXem = false
                };
                _context.ThongBaos.Add(notif);
                await _context.SaveChangesAsync();

                // SignalR Updates
                await _hubContext.Clients.Group("Doctors").SendAsync("ReceiveStatusChange", new
                {
                    maLich = appt.MaLich,
                    trangThaiMoi = "Đã hủy",
                    noiDung = $"Lịch hẹn #{appt.MaLich} đã bị hủy bởi bệnh nhân."
                });

                await _hubContext.Clients.Group($"User_{appt.MaBenhNhan}").SendAsync("ReceiveAppointmentUpdate", "Đã hủy lịch thành công");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // --- DTO Classes ---
        public class BookAppointmentRequest
        {
            public string SelectedDoctorId { get; set; }
            public string SelectedDate { get; set; }
            public string SelectedTime { get; set; }
            public string Symptoms { get; set; }
        }

        public class CancelRequest
        {
            public int Id { get; set; }
        }
    }
}