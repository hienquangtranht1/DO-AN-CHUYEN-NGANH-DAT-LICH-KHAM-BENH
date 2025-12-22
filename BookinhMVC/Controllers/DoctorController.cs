using Microsoft.AspNetCore.Mvc;
using BookinhMVC.Models;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Net.Mime; // Cần thêm cái này để xử lý ảnh trong email
using QRCoder;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Hubs;

namespace BookinhMVC.Controllers
{
    public class DoctorController : Controller
    {
        private readonly BookingContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<BookingHub> _hubContext;

        public DoctorController(BookingContext context, IWebHostEnvironment env, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _env = env;
            _hubContext = hubContext;
        }

        // ===================================================================
        // 1. MIDDLEWARE & AUTHENTICATION
        // ===================================================================
        private bool IsDoctorLoggedIn()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var id = HttpContext.Session.GetInt32("DoctorId");

            if (role != "BacSi" || id == null) return false;

            var user = _context.Set<NguoiDung>().FirstOrDefault(u => u.MaNguoiDung == id && u.VaiTro == "BacSi");
            return user != null;
        }

        [HttpGet]
        public IActionResult Login() => View("Login");

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username && u.VaiTro == "BacSi");
            if (user != null)
            {
                var hasher = new PasswordHasher<NguoiDung>();
                var result = hasher.VerifyHashedPassword(user, user.MatKhau, password);
                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetInt32("DoctorId", user.MaNguoiDung);
                    HttpContext.Session.SetString("UserRole", "BacSi");
                    HttpContext.Session.SetString("DoctorName", user.TenDangNhap);

                    var bacSi = _context.BacSis.FirstOrDefault(b => b.MaNguoiDung == user.MaNguoiDung);
                    if (bacSi != null)
                    {
                        HttpContext.Session.SetInt32("MaBacSi", bacSi.MaBacSi);
                        HttpContext.Session.SetString("DoctorImage", bacSi.HinhAnhBacSi ?? "default.jpg");
                    }
                    return RedirectToAction("Appointments");
                }
            }
            ViewBag.Error = "Sai tài khoản hoặc không phải bác sĩ.";
            return View("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("DoctorId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("DoctorName");
            HttpContext.Session.Remove("MaBacSi");
            HttpContext.Session.Remove("DoctorImage");
            return RedirectToAction("Login", "Doctor");
        }

        // ===================================================================
        // 2. PROFILE MANAGEMENT
        // ===================================================================
        [HttpGet]
        public IActionResult Profile()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maNguoiDung = HttpContext.Session.GetInt32("DoctorId");

            var bacSi = _context.Set<BacSi>()
                .Include(b => b.Khoa)
                .FirstOrDefault(b => b.MaNguoiDung == maNguoiDung);

            if (bacSi == null) return RedirectToAction("Login");
            return View(bacSi);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(int MaBacSi, string HoTen, string SoDienThoai, string Email, string MoTa, IFormFile profileImage)
        {
            var doctor = await _context.BacSis.FindAsync(MaBacSi);
            if (doctor == null) return RedirectToAction("Profile");

            doctor.HoTen = HoTen;
            doctor.SoDienThoai = SoDienThoai;
            doctor.Email = Email;
            doctor.MoTa = MoTa;

            if (profileImage != null && profileImage.Length > 0)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadPath = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(profileImage.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(doctor.HinhAnhBacSi) && doctor.HinhAnhBacSi != "default.jpg")
                {
                    var oldPath = Path.Combine(uploadPath, doctor.HinhAnhBacSi);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                doctor.HinhAnhBacSi = fileName;
                HttpContext.Session.SetString("DoctorImage", fileName);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestChangePasswordOtp()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (!IsDoctorLoggedIn()) return Json(new { success = false, message = "Chưa đăng nhập." });

            var bacSi = await _context.BacSis.FirstOrDefaultAsync(b => b.MaNguoiDung == doctorId);

            if (bacSi == null || string.IsNullOrEmpty(bacSi.Email)) return Json(new { success = false, message = "Không tìm thấy email." });

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ChangePasswordOtp", otp);
            HttpContext.Session.SetString("ChangePasswordOtpTime", DateTime.Now.ToString());

            try
            {
                var smtpUser = "hienquangtranht1@gmail.com";
                var smtpPass = "aigh nsyp dgyu emhc";
                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };
                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser, "BỆNH VIỆN FOUR_ROCK"),
                    Subject = "OTP Đổi mật khẩu",
                    Body = $"Mã OTP của bạn là: {otp}"
                };
                mail.To.Add(bacSi.Email);
                await smtp.SendMailAsync(mail);
                return Json(new { success = true, message = "Đã gửi OTP." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi gửi mail: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword, string verificationCode)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var doctor = await _context.NguoiDungs.FindAsync(doctorId);

            var hasher = new PasswordHasher<NguoiDung>();
            if (hasher.VerifyHashedPassword(doctor, doctor.MatKhau, oldPassword) != PasswordVerificationResult.Success)
            {
                TempData["cp_message"] = "Mật khẩu cũ sai.";
                return RedirectToAction("ChangePassword");
            }

            var sessionOtp = HttpContext.Session.GetString("ChangePasswordOtp");
            if (verificationCode != sessionOtp)
            {
                TempData["cp_message"] = "Mã OTP sai.";
                return RedirectToAction("ChangePassword");
            }

            doctor.MatKhau = hasher.HashPassword(doctor, newPassword);
            await _context.SaveChangesAsync();
            TempData["cp_message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }

        // ===================================================================
        // 3. APPOINTMENTS (MVC) - TÍCH HỢP SIGNALR
        // ===================================================================
        public async Task<IActionResult> Appointments(DateTime? date)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");
            var filterDate = date ?? DateTime.Today;
            ViewData["FilterDate"] = filterDate.ToString("yyyy-MM-dd");

            var workSchedules = await _context.Set<LichLamViec>()
                .Where(lv => lv.MaBacSi == maBacSi && lv.NgayLamViec == filterDate.Date && lv.TrangThai == "Đã xác nhận")
                .ToListAsync();

            TimeSpan actualStartTime = workSchedules.Any() ? workSchedules.Min(lv => lv.GioBatDau) : new TimeSpan(7, 0, 0);
            TimeSpan actualEndTime = workSchedules.Any() ? workSchedules.Max(lv => lv.GioKetThuc) : new TimeSpan(17, 30, 0);

            ViewData["ActualStartTime"] = actualStartTime;
            ViewData["ActualEndTime"] = actualEndTime;
            ViewData["HasSchedule"] = workSchedules.Any();

            var appointments = await (from lh in _context.Set<LichHen>()
                                      join bn in _context.Set<BenhNhan>() on lh.MaBenhNhan equals bn.MaBenhNhan
                                      where lh.MaBacSi == maBacSi && lh.NgayGio.Date == filterDate.Date
                                      select new AppointmentViewModel
                                      {
                                          MaLich = lh.MaLich,
                                          HoTenBenhNhan = bn.HoTen,
                                          DiaChi = bn.DiaChi,
                                          GioiTinh = bn.GioiTinh,
                                          NgayGio = lh.NgayGio,
                                          TrieuChung = lh.TrieuChung,
                                          TrangThai = lh.TrangThai,
                                      }).ToListAsync();

            return View("Appointments", appointments.OrderBy(a => a.NgayGio).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointment(int id, string status)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");

            var appointment = await _context.Set<LichHen>()
                .Include(l => l.BenhNhan)
                .Include(l => l.BacSi)
                .FirstOrDefaultAsync(l => l.MaLich == id);

            if (appointment != null)
            {
                bool isConfirmedNow = (appointment.TrangThai != "Đã xác nhận" && status == "Đã xác nhận");
                appointment.TrangThai = status;

                // 1. Lưu trạng thái & Thông báo DB
                var notif = new ThongBao
                {
                    MaNguoiDung = appointment.MaBenhNhan,
                    TieuDe = isConfirmedNow ? "Lịch hẹn đã được xác nhận" : $"Trạng thái lịch hẹn: {status}",
                    NoiDung = $"BS {appointment.BacSi.HoTen} đã chuyển trạng thái lịch khám {appointment.NgayGio:HH:mm dd/MM} sang: {status}.",
                    NgayTao = DateTime.Now,
                    DaXem = false,
                    MaLichHen = appointment.MaLich
                };
                _context.Add(notif);
                await _context.SaveChangesAsync();

                // 2. SIGNALR REAL-TIME
                await _hubContext.Clients.Group($"User_{appointment.MaBenhNhan}").SendAsync("ReceiveStatusChange", new
                {
                    maLich = appointment.MaLich,
                    trangThaiMoi = status,
                    tieuDe = notif.TieuDe,
                    noiDung = notif.NoiDung
                });

                // 3. Gửi Email (HTML + QR Code đẹp)
                string patientEmail = appointment.BenhNhan.Email;
                if (!string.IsNullOrEmpty(patientEmail))
                {
                    try
                    {
                        if (isConfirmedNow)
                        {
                            // Tạo QR Code xuống dòng rõ ràng
                            string qrData = $"--- PHIẾU KHÁM BỆNH ---\n" +
                                            $"Mã lịch: LH-{appointment.MaLich}\n" +
                                            $"Bệnh nhân: {appointment.BenhNhan.HoTen}\n" +
                                            $"Bác sĩ: {appointment.BacSi.HoTen}\n" +
                                            $"Thời gian: {appointment.NgayGio:HH:mm - dd/MM/yyyy}\n" +
                                            $"Trạng thái: Đã xác nhận\n" +
                                            $"-----------------------";

                            string qrBase64 = GenerateQrCodeAsBase64(qrData);

                            // Gửi email xác nhận kèm QR
                            await SendConfirmationEmailAsync(patientEmail, appointment.BenhNhan.HoTen, appointment.BacSi.HoTen, appointment.NgayGio.ToString("HH:mm dd/MM/yyyy"), qrBase64);
                        }
                        else
                        {
                            // Gửi email cập nhật trạng thái khác (Hủy, chờ...)
                            await SendStatusUpdateEmailAsync(patientEmail, appointment.BenhNhan.HoTen, appointment.BacSi.HoTen, appointment.NgayGio.ToString("HH:mm dd/MM/yyyy"), status);
                        }
                    }
                    catch { /* Bỏ qua lỗi email để không crash */ }
                }
            }
            return RedirectToAction("Appointments", new { date = appointment?.NgayGio.ToString("yyyy-MM-dd") });
        }

        // ===================================================================
        // 4. WORK SCHEDULE & MEDICAL RECORDS
        // ===================================================================
        public IActionResult WorkSchedule(DateTime? weekStart)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");
            var start = weekStart ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var end = start.AddDays(6);

            var schedules = _context.Set<LichLamViec>()
                .Where(l => l.MaBacSi == maBacSi && l.NgayLamViec >= start && l.NgayLamViec <= end)
                .ToList();

            ViewBag.WeekStart = start;
            ViewBag.WeekEnd = end;
            return View("WorkSchedule", schedules);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWorkSchedule(int Id, string Ngay, string GioBatDau, string GioKetThuc, string actionType)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");
            DateTime date = DateTime.Parse(Ngay);
            TimeSpan start = TimeSpan.Parse(GioBatDau);
            TimeSpan end = TimeSpan.Parse(GioKetThuc);

            if (actionType == "delete")
            {
                var s = await _context.Set<LichLamViec>().FindAsync(Id);
                if (s != null) _context.Remove(s);
            }
            else if (actionType == "add")
            {
                var lich = new LichLamViec { MaBacSi = maBacSi.Value, NgayLamViec = date, GioBatDau = start, GioKetThuc = end, TrangThai = "Chờ xác nhận", NgayTao = DateTime.Now, ThuTrongTuan = ((int)date.DayOfWeek == 0 ? "8" : ((int)date.DayOfWeek + 1).ToString()) };
                _context.Add(lich);
            }
            else if (actionType == "update")
            {
                var s = await _context.Set<LichLamViec>().FindAsync(Id);
                if (s != null) { s.GioBatDau = start; s.GioKetThuc = end; s.TrangThai = "Chờ xác nhận"; }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("WorkSchedule", new { weekStart = date.AddDays(-(int)date.DayOfWeek + 1).ToString("yyyy-MM-dd") });
        }

        public IActionResult MedicalRecords(string search)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");

            var query = _context.Set<HoSoBenhAn>()
                .Include(h => h.BenhNhan)
                .Include(h => h.BacSi)
                .Where(h => h.MaBacSi == maBacSi);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(h => h.BenhNhan.HoTen.Contains(search));

            var records = query.Select(h => new MedicalRecordViewModel
            {
                MaHoSo = h.MaHoSo,
                TenBenhNhan = h.BenhNhan.HoTen,
                TenBacSi = h.BacSi.HoTen,
                NgayKham = h.NgayKham,
                ChanDoan = h.ChanDoan,
                PhuongAnDieuTri = h.PhuongAnDieuTri
            }).ToList();

            return View("MedicalRecords", records);
        }

        // ===================================================================
        // 5. API ACTIONS
        // ===================================================================

        [HttpGet]
        [Route("api/doctors")]
        public async Task<IActionResult> GetDoctors(int page = 1, int pageSize = 10, string search = "")
        {
            var query = _context.BacSis
                .Include(d => d.Khoa)
                .Where(d => !string.IsNullOrEmpty(d.HoTen));

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(d => d.HoTen.ToLower().Contains(search) ||
                                         (d.Khoa != null && d.Khoa.TenKhoa.ToLower().Contains(search)));
            }

            var totalItems = await query.CountAsync();
            var doctors = await query
                .OrderByDescending(d => d.MaBacSi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = doctors.Select(d => new
            {
                id = d.MaBacSi,
                name = d.HoTen,
                specialty = d.Khoa?.TenKhoa ?? "Chuyên khoa khác",
                imageUrl = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi),
                phone = d.SoDienThoai
            }).ToList();

            return Ok(new
            {
                data = result,
                total = totalItems,
                page = page,
                hasNext = (page * pageSize) < totalItems
            });
        }

        [HttpGet]
        [Route("api/doctors/featured")]
        public async Task<IActionResult> GetFeaturedDoctors()
        {
            var list = await _context.BacSis.Include(d => d.Khoa).Where(d => !string.IsNullOrEmpty(d.HoTen)).OrderBy(d => Guid.NewGuid()).Take(6).ToListAsync();
            var result = list.Select(d =>
            {
                var imgPath = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi);
                return new { id = d.MaBacSi, name = d.HoTen, specialty = d.Khoa?.TenKhoa ?? "", phone = d.SoDienThoai, email = d.Email, description = d.MoTa, imageUrl = imgPath };
            }).ToList();
            return Ok(result);
        }

        [HttpGet]
        [Route("api/doctors/{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var d = await _context.BacSis.Include(b => b.Khoa).FirstOrDefaultAsync(b => b.MaBacSi == id);
            if (d == null) return NotFound();
            var imgPath = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi);
            var result = new { id = d.MaBacSi, name = d.HoTen, specialty = d.Khoa?.TenKhoa ?? "", phone = d.SoDienThoai, email = d.Email, description = d.MoTa, imageUrl = imgPath };
            return Ok(result);
        }

        private string? BuildRelativeImagePathIfExists(int maBacSi, string? storedFileName)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsFolder)) return null;
            if (!string.IsNullOrEmpty(storedFileName))
            {
                var path = Path.Combine(uploadsFolder, storedFileName);
                if (System.IO.File.Exists(path)) return $"/uploads/{storedFileName}";
            }
            var exts = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var ext in exts)
            {
                var file = Path.Combine(uploadsFolder, $"{maBacSi}{ext}");
                if (System.IO.File.Exists(file)) return $"/uploads/{maBacSi}{ext}";
            }
            return null;
        }

        // ===================================================================
        // 6. QA & HELPERS (EMAIL HTML, QR)
        // ===================================================================
        private string GenerateQrCodeAsBase64(string text)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }

        // ĐÃ CẬP NHẬT: Gửi Email xác nhận giao diện đẹp + QR đính kèm inline
        private async Task SendConfirmationEmailAsync(string toEmail, string patientName, string doctorName, string time, string qrBase64)
        {
            var smtpUser = "hienquangtranht1@gmail.com";
            var smtpPass = "aigh nsyp dgyu emhc";

            var mail = new MailMessage();
            mail.From = new MailAddress(smtpUser, "BỆNH VIỆN FOUR ROCK");
            mail.To.Add(toEmail);
            mail.Subject = "[FOUR ROCK] Xác nhận lịch hẹn khám bệnh";
            mail.IsBodyHtml = true;

            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #007bff; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>XÁC NHẬN LỊCH HẸN</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Xin chào <strong>{patientName}</strong>,</p>
                    <p>Chúng tôi vui mừng thông báo lịch hẹn khám bệnh của bạn tại <strong>Bệnh viện Four Rock</strong> đã được bác sĩ xác nhận.</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                        <tr style='background-color: #f9f9f9;'>
                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Bác sĩ phụ trách:</strong></td>
                            <td style='padding: 10px; border: 1px solid #ddd; color: #0056b3;'>{doctorName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Thời gian:</strong></td>
                            <td style='padding: 10px; border: 1px solid #ddd; color: #d9534f;'>{time}</td>
                        </tr>
                    </table>

                    <div style='text-align: center; margin: 20px 0;'>
                        <p>Vui lòng đưa mã QR dưới đây tại quầy tiếp tân để làm thủ tục nhanh chóng:</p>
                        <img src='cid:qrImage' style='width: 200px; height: 200px; border: 2px solid #333; padding: 5px;'/>
                    </div>

                    <p style='font-style: italic; color: #666;'>
                        * Lưu ý: Vui lòng đến trước giờ hẹn 15 phút để hoàn tất thủ tục hành chính. Nếu không thể đến, vui lòng hủy lịch hẹn trên hệ thống.
                    </p>
                </div>
                <div style='background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                    <p>Bệnh viện Four Rock - Vì sức khỏe cộng đồng</p>
                    <p>Hotline: 1900 xxxx | Email: contact@fourrock.com</p>
                </div>
            </div>";

            var view = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

            // Xử lý ảnh QR đính kèm inline
            var qrBytes = Convert.FromBase64String(qrBase64);
            var stream = new MemoryStream(qrBytes);
            var linked = new LinkedResource(stream, "image/png") { ContentId = "qrImage" };
            view.LinkedResources.Add(linked);

            mail.AlternateViews.Add(view);

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };
            await smtp.SendMailAsync(mail);
        }

        // ĐÃ CẬP NHẬT: Gửi Email cập nhật trạng thái (Hủy/Dời) giao diện đẹp
        private async Task SendStatusUpdateEmailAsync(string toEmail, string patientName, string doctorName, string time, string status)
        {
            var smtpUser = "hienquangtranht1@gmail.com";
            var smtpPass = "aigh nsyp dgyu emhc";

            var mail = new MailMessage();
            mail.From = new MailAddress(smtpUser, "BỆNH VIỆN FOUR ROCK");
            mail.To.Add(toEmail);
            mail.Subject = $"[FOUR ROCK] Cập nhật trạng thái lịch hẹn: {status.ToUpper()}";
            mail.IsBodyHtml = true;

            string statusColor = status.Contains("Hủy") ? "#dc3545" : "#28a745"; // Đỏ nếu hủy, Xanh nếu khác

            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px;'>
                <div style='background-color: {statusColor}; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>THÔNG BÁO TỪ BÁC SĨ</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Xin chào <strong>{patientName}</strong>,</p>
                    <p>Lịch hẹn khám bệnh của bạn với <strong>BS {doctorName}</strong> vào lúc <strong>{time}</strong> vừa có sự thay đổi.</p>
                    
                    <div style='background-color: #f8f9fa; padding: 15px; border-left: 5px solid {statusColor}; margin: 20px 0;'>
                        <p style='margin: 0;'>Trạng thái mới: <strong style='font-size: 18px; color: {statusColor};'>{status}</strong></p>
                    </div>

                    <p>Vui lòng truy cập hệ thống website để kiểm tra chi tiết hoặc đặt lại lịch hẹn mới nếu cần thiết.</p>
                </div>
                <div style='background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                    <p>Trân trọng,<br>Đội ngũ Bệnh viện Four Rock</p>
                </div>
            </div>";

            mail.Body = htmlBody;

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };
            await smtp.SendMailAsync(mail);
        }

        [HttpGet]
        public async Task<IActionResult> Question()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");

            var questions = await _context.Questions
                .Include(q => q.User)
                .Where(q => q.DoctorId == maBacSi)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(questions);
        }

        [HttpPost]
        public async Task<IActionResult> Answer(int questionId, string answer)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var q = await _context.Questions.FindAsync(questionId);
            if (q != null)
            {
                q.Answer = answer;
                q.Status = "Đã trả lời";
                q.AnsweredAt = DateTime.Now;
                _context.Questions.Update(q);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã gửi câu trả lời thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy câu hỏi.";
            }
            return RedirectToAction("Question");
        }
    }
}