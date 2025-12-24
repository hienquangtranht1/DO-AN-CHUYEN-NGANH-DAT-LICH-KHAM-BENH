using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using BookinhMVC.Models;
using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Hubs;
using BookinhMVC.Helpers;

namespace BookinhMVC.Controllers
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly BookingContext _context;
        private readonly PasswordHasher<NguoiDung> _passwordHasher;
        private readonly IHubContext<BookingHub> _hubContext;

        // Cấu hình Email
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "hienquangtranht1@gmail.com";
        private readonly string _smtpPass = "aigh nsyp dgyu emhc";

        public UserController(BookingContext context, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
            _passwordHasher = new PasswordHasher<NguoiDung>();
        }

        // ---------------------------------------------------------
        // HELPER: Gửi Email
        // ---------------------------------------------------------
        private async Task SendMailAsync(string toEmail, string subject, string bodyPlain)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BỆNH VIỆN FOUR_ROCK", _smtpUser));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = bodyPlain };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            }
        }

        // =========================================================
        // PHẦN 1: WEB MVC (Trả về View cho trình duyệt)
        // =========================================================

        #region Authentication (Login/Register/Logout)

        [HttpGet("Login")]
        public IActionResult Login() => View();

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == username);
            if (user != null && _passwordHasher.VerifyHashedPassword(user, user.MatKhau, password) == PasswordVerificationResult.Success)
            {
                // Lưu Session
                HttpContext.Session.SetInt32("UserId", user.MaNguoiDung);
                HttpContext.Session.SetString("UserRole", user.VaiTro);

                if (user.VaiTro == "Bệnh nhân")
                {
                    var p = await _context.BenhNhans.FindAsync(user.MaNguoiDung);
                    if (p != null) HttpContext.Session.SetString("PatientName", p.HoTen);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu.");
            return View();
        }

        [HttpPost("/Logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "User");
        }

        [AllowAnonymous]
        [HttpGet("Register")]
        public IActionResult Register() => View();

        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string fullname, DateTime dob,
           string gender, string phone, string email, string address, string soBaoHiem)
        {
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == username))
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại.");
                return View();
            }

            if (await _context.BenhNhans.AnyAsync(b => b.Email == email))
            {
                ModelState.AddModelError("", "Email đã được sử dụng.");
                return View();
            }

            string otp = new Random().Next(100000, 999999).ToString();
            var registrationData = new RegistrationModel
            {
                username = username,
                password = password,
                fullname = fullname,
                dob = dob,
                gender = gender,
                phone = phone,
                email = email,
                address = address,
                soBaoHiem = soBaoHiem,
                otp = otp
            };

            HttpContext.Session.SetString("RegistrationData", JsonSerializer.Serialize(registrationData));
            HttpContext.Session.SetString("RegistrationOtp", otp);
            HttpContext.Session.SetString("RegistrationOtpTime", DateTime.UtcNow.ToString("o"));

            await SendMailAsync(email, "Xác nhận đăng ký", $"Mã OTP của bạn là: {otp}");
            return RedirectToAction("VerifyOtp");
        }

        [AllowAnonymous]
        [HttpGet("VerifyOtp")]
        public IActionResult VerifyOtp() => View();

        // -----------------------------------------------------------
        // FIX LỖI QUAN TRỌNG TẠI ĐÂY (HỖ TRỢ 6 Ô INPUT & BẮT LỖI DB)
        // -----------------------------------------------------------
        [HttpPost("VerifyOtp")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(Microsoft.AspNetCore.Http.IFormCollection form)
        {
            // Collect any posted OTP parts (supports otp1, otp2, otp-3, otp[] etc.)
            var otpKeys = form.Keys
                .Where(k => k.StartsWith("otp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // If keys are like otp[0], otp[1] or otp1..otp6 sorting by numeric suffix helps
            var orderedKeys = otpKeys
                .OrderBy(k =>
                {
                    var digits = new string(k.Where(char.IsDigit).ToArray());
                    return int.TryParse(digits, out var n) ? n : int.MaxValue;
                })
                .ToList();

            // Fallback: if no otp keys found, try common single-field name "otp"
            if (!orderedKeys.Any() && form.ContainsKey("otp"))
            {
                orderedKeys.Add("otp");
            }

            var otpParts = orderedKeys.Select(k => form[k].ToString().Trim()).ToArray();
            string enteredOtp = string.Concat(otpParts);

            // Basic validation
            if (string.IsNullOrWhiteSpace(enteredOtp) || enteredOtp.Length != 6)
            {
                TempData["Error"] = "Vui lòng nhập đủ 6 chữ số OTP.";
                return View();
            }

            // Read session
            var sessionOtp = HttpContext.Session.GetString("RegistrationOtp");
            var registrationJson = HttpContext.Session.GetString("RegistrationData");
            var otpTimeStr = HttpContext.Session.GetString("RegistrationOtpTime");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(registrationJson) || string.IsNullOrEmpty(otpTimeStr))
            {
                TempData["reg_error"] = "Hết hạn phiên đăng ký. Vui lòng làm lại.";
                return RedirectToAction("Register");
            }

            if (!string.Equals(enteredOtp, sessionOtp, StringComparison.Ordinal))
            {
                TempData["Error"] = "Mã OTP không chính xác. Vui lòng kiểm tra lại.";
                return View();
            }

            if (DateTime.TryParse(otpTimeStr, out var otpTime) && (DateTime.UtcNow - otpTime).TotalMinutes > 10)
            {
                TempData["reg_error"] = "Mã OTP đã hết hạn.";
                return RedirectToAction("Register");
            }

            var regData = JsonSerializer.Deserialize<RegistrationModel>(registrationJson);

            // Transaction: create user, patient, wallet
            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new NguoiDung
                {
                    TenDangNhap = regData.username,
                    VaiTro = "Bệnh nhân",
                    NgayTao = DateTime.Now
                };
                user.MatKhau = _passwordHasher.HashPassword(user, regData.password);
                _context.NguoiDungs.Add(user);
                await _context.SaveChangesAsync(); // ensure user.Id generated

                var patient = new BenhNhan
                {
                    MaBenhNhan = user.MaNguoiDung,
                    HoTen = regData.fullname,
                    NgaySinh = regData.dob,
                    GioiTinh = regData.gender,
                    SoDienThoai = regData.phone,
                    Email = regData.email,
                    DiaChi = regData.address,
                    SoBaoHiem = regData.soBaoHiem ?? "",
                    HinhAnhBenhNhan = "default.jpg",
                    NgayTao = DateTime.Now
                };
                _context.BenhNhans.Add(patient);
                await _context.SaveChangesAsync();

                var wallet = new TaiKhoanBenhNhan
                {
                    MaBenhNhan = user.MaNguoiDung,
                    SoDuHienTai = 0,
                    NgayCapNhatCuoi = DateTime.Now
                };
                _context.TaiKhoanBenhNhan.Add(wallet);
                await _context.SaveChangesAsync();

                await trans.CommitAsync();

                HttpContext.Session.Remove("RegistrationData");
                HttpContext.Session.Remove("RegistrationOtp");
                HttpContext.Session.Remove("RegistrationOtpTime");

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                var realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Lỗi lưu dữ liệu: " + realError;
                return View();
            }
        }

        #endregion

        #region Password Management

        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword() => View();

        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            var patient = await _context.BenhNhans.FirstOrDefaultAsync(b => b.Email == email);
            if (patient == null)
            {
                ModelState.AddModelError("", "Email không tồn tại.");
                return View();
            }

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ForgotPasswordOtp", otp);
            HttpContext.Session.SetString("ForgotPasswordEmail", email);
            HttpContext.Session.SetString("ForgotPasswordOtpTime", DateTime.UtcNow.ToString("o"));

            await SendMailAsync(email, "Quên mật khẩu - Mã OTP", $"Mã OTP của bạn: {otp}");
            TempData["Message"] = "Đã gửi mã OTP tới email.";
            return RedirectToAction("ResetPassword");
        }

        [AllowAnonymous]
        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword() => View();

        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string otp, string newPassword, string confirmPassword)
        {
            var sessionOtp = HttpContext.Session.GetString("ForgotPasswordOtp");
            var email = HttpContext.Session.GetString("ForgotPasswordEmail");
            var otpTimeStr = HttpContext.Session.GetString("ForgotPasswordOtpTime");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Phiên OTP đã hết hạn.";
                return RedirectToAction("ForgotPassword");
            }

            if (otp != sessionOtp)
            {
                ModelState.AddModelError("", "Mã OTP không đúng.");
                return View();
            }

            if (DateTime.TryParse(otpTimeStr, out var t) && (DateTime.UtcNow - t).TotalMinutes > 15)
            {
                TempData["Error"] = "Mã OTP đã hết hạn.";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới không khớp.");
                return View();
            }

            var patient = await _context.BenhNhans.FirstOrDefaultAsync(b => b.Email == email);
            var user = await _context.NguoiDungs.FindAsync(patient.MaBenhNhan);

            user.MatKhau = _passwordHasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ForgotPasswordOtp");
            HttpContext.Session.Remove("ForgotPasswordEmail");

            TempData["Message"] = "Đặt lại mật khẩu thành công.";
            return RedirectToAction("Login");
        }

        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || role != "Bệnh nhân")
            {
                TempData["Error"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login", "User");
            }
            return View();
        }

        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.NguoiDungs.FindAsync(userId);

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.MatKhau, oldPassword);
            if (verifyResult == PasswordVerificationResult.Success)
            {
                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                    return View();
                }

                user.MatKhau = _passwordHasher.HashPassword(user, newPassword);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Logout");
            }

            ModelState.AddModelError("", "Mật khẩu cũ không đúng.");
            return View();
        }

        #endregion

        #region Profile & Data (Appointments/Notifications)

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var patient = await _context.BenhNhans.FindAsync(userId);
            if (patient == null) return RedirectToAction("Login");

            return View(patient);
        }

        [HttpGet("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var patient = await _context.BenhNhans.FindAsync(userId);
            return View(patient);
        }

        [HttpPost("UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string hoTen, DateTime ngaySinh, string gioiTinh, string soDienThoai, string email, string diaChi, string soBaoHiem, IFormFile hinhAnhBenhNhan)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var patient = await _context.BenhNhans.FindAsync(userId);

            patient.HoTen = hoTen;
            patient.NgaySinh = ngaySinh;
            patient.GioiTinh = gioiTinh;
            patient.SoDienThoai = soDienThoai;
            patient.Email = email;
            patient.DiaChi = diaChi;
            patient.SoBaoHiem = soBaoHiem;

            if (hinhAnhBenhNhan != null && hinhAnhBenhNhan.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{hinhAnhBenhNhan.FileName}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await hinhAnhBenhNhan.CopyToAsync(stream);
                }
                patient.HinhAnhBenhNhan = fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        [HttpGet("Appointments")]
        public async Task<IActionResult> Appointments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var list = await _context.LichHens
                .Include(l => l.BacSi)
                .Where(l => l.MaBenhNhan == userId)
                .OrderByDescending(l => l.NgayGio)
                .ToListAsync();

            // Load wallet balance (TaiKhoanBenhNhan)
            decimal balance = 0m;
            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(w => w.MaBenhNhan == userId.Value);
            if (wallet != null) balance = wallet.SoDuHienTai;

            // Determine which appointments already have a "Thanh toán lịch hẹn" transaction
            var appointmentIds = list.Select(l => l.MaLich).ToList();
            var paidIds = await _context.GiaoDichThanhToan
                .Where(g => g.MaBenhNhan == userId.Value && g.MaLich != null && appointmentIds.Contains(g.MaLich.Value) && g.LoaiGiaoDich == "Thanh toán lịch hẹn")
                .Select(g => g.MaLich!.Value)
                .ToListAsync();

            ViewBag.LichHens = list;
            ViewBag.Balance = balance;
            ViewBag.PaidAppointments = paidIds.ToHashSet();

            return View();
        }

        // New: Pay for appointment (deduct wallet) - price fixed to 50,000 VND
        [HttpPost("/PayAppointment")] // <-- absolute path
        public async Task<IActionResult> PayAppointment([FromForm] int appointmentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var appt = await _context.LichHens.FirstOrDefaultAsync(l => l.MaLich == appointmentId);
            if (appt == null || appt.MaBenhNhan != userId.Value)
                return Json(new { success = false, message = "Lịch hẹn không tồn tại hoặc không thuộc về bạn." });

            // Only allow payment for confirmed appointments
            if (!string.Equals(appt.TrangThai?.Trim(), "Đã xác nhận", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Chỉ có lịch hẹn đã xác nhận mới được thanh toán." });
            }

            const decimal price = 50000m;

            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(w => w.MaBenhNhan == userId.Value);
            if (wallet == null) return Json(new { success = false, message = "Ví không tồn tại. Vui lòng nạp tiền." });

            if (wallet.SoDuHienTai < price)
            {
                return Json(new { success = false, insufficient = true, message = "Số dư không đủ. Vui lòng nạp thêm." });
            }

            using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Deduct
                wallet.SoDuHienTai -= price;
                wallet.NgayCapNhatCuoi = DateTime.Now;
                _context.TaiKhoanBenhNhan.Update(wallet);

                // Create transaction record
                var txn = new GiaoDichThanhToan
                {
                    MaBenhNhan = userId.Value,
                    MaLich = appointmentId,
                    SoTien = price,
                    NgayGiaoDich = DateTime.Now,
                    LoaiGiaoDich = "Thanh toán lịch hẹn",
                    TrangThai = "Thành công",
                    NoiDung = $"Thanh toán lịch hẹn #{appointmentId}",
                    MaThamChieu = TransactionHelper.GenerateTransactionCode("Thanh toán lịch hẹn"),
                    PhuongThucThanhToan = "Ví nội bộ"
                };
                _context.GiaoDichThanhToan.Add(txn);

                // Notification for patient
                var notif = new ThongBao
                {
                    MaNguoiDung = userId.Value,
                    TieuDe = "Thanh toán thành công",
                    NoiDung = $"Bạn đã thanh toán {price:N0} VND cho lịch hẹn #{appointmentId}.",
                    NgayTao = DateTime.Now,
                    MaLichHen = appointmentId,
                    DaXem = false
                };
                _context.ThongBaos.Add(notif);

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();

                // Real-time signal to patient group
                await _hubContext.Clients.Group($"User_{userId.Value}").SendAsync("PaymentUpdated", new
                {
                    appointmentId = appointmentId,
                    newBalance = wallet.SoDuHienTai,
                    message = $"Thanh toán {price:N0} VND cho lịch hẹn #{appointmentId} thành công."
                });

                return Json(new { success = true, newBalance = wallet.SoDuHienTai, message = "Thanh toán thành công." });
            }
            catch (Exception ex)
            {
                await dbTrans.RollbackAsync();
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpGet("Notifications")]
        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var list = await _context.ThongBaos
                .Where(t => t.MaNguoiDung == userId.Value)
                .OrderByDescending(t => t.NgayTao)
                .Take(100)
                .ToListAsync();

            return View(list);
        }

        #endregion

        // =========================================================
        // PHẦN 2: AJAX & WEB API (Cho Menu/Navbar)
        // =========================================================

        [HttpGet("Notifications/Count")]
        public async Task<IActionResult> NotificationsCount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, unread = 0 });

            var unread = await _context.ThongBaos
                .Where(t => t.MaNguoiDung == userId.Value && !t.DaXem)
                .CountAsync();

            return Json(new { success = true, unread });
        }

        [HttpGet("Notifications/List")]
        public async Task<IActionResult> NotificationsList(int take = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            var list = await _context.ThongBaos
                .Where(t => t.MaNguoiDung == userId.Value)
                .OrderByDescending(t => t.NgayTao)
                .Take(take)
                .Select(t => new NotificationDto
                {
                    Id = t.MaThongBao,
                    Title = t.TieuDe,
                    Content = t.NoiDung,
                    CreatedAt = t.NgayTao,
                    IsRead = t.DaXem,
                    RelatedAppointmentId = t.MaLichHen
                })
                .ToListAsync();

            return Json(new { success = true, data = list });
        }

        [HttpPost("Notifications/MarkRead")]
        public async Task<IActionResult> NotificationsMarkRead([FromBody] MarkReadRequest req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            var item = await _context.ThongBaos.FindAsync(req.Id);
            if (item != null && item.MaNguoiDung == userId.Value && !item.DaXem)
            {
                item.DaXem = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost("Notifications/MarkAllRead")]
        public async Task<IActionResult> NotificationsMarkAllRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            var items = await _context.ThongBaos.Where(t => t.MaNguoiDung == userId.Value && !t.DaXem).ToListAsync();
            foreach (var it in items) it.DaXem = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost("CheckUniqueness")]
        public async Task<IActionResult> CheckUniqueness(string fieldName, string fieldValue)
        {
            bool isUnique = true;
            string errorMessage = "";

            switch (fieldName?.ToLower())
            {
                case "username":
                    if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == fieldValue))
                    { errorMessage = "Tên đăng nhập đã được sử dụng."; isUnique = false; }
                    break;
                case "email":
                    if (await _context.BenhNhans.AnyAsync(b => b.Email == fieldValue))
                    { errorMessage = "Email đã được sử dụng."; isUnique = false; }
                    break;
                case "sobaohiem":
                    if (await _context.BenhNhans.AnyAsync(b => b.SoBaoHiem == fieldValue))
                    { errorMessage = "Mã BHYT đã được sử dụng."; isUnique = false; }
                    break;
                case "phone":
                    if (string.IsNullOrWhiteSpace(fieldValue) || !System.Text.RegularExpressions.Regex.IsMatch(fieldValue, @"^0\d{9}$"))
                    { errorMessage = "Số điện thoại không hợp lệ."; isUnique = false; }
                    break;
            }
            return Json(new { isUnique, errorMessage });
        }

        // =========================================================
        // PHẦN 3: MOBILE API (Trả về JSON cho App Flutter)
        // =========================================================

        [HttpPost("/api/user/login")]
        public async Task<IActionResult> ApiLogin([FromBody] LoginRequestDto body)
        {
            if (body == null) return BadRequest(new { message = "Dữ liệu trống" });

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == body.Username);
            if (user != null && _passwordHasher.VerifyHashedPassword(user, user.MatKhau, body.Password) == PasswordVerificationResult.Success)
            {
                HttpContext.Session.SetInt32("UserId", user.MaNguoiDung);
                HttpContext.Session.SetString("UserRole", user.VaiTro);

                if (user.VaiTro == "Bệnh nhân")
                {
                    var p = await _context.BenhNhans.FindAsync(user.MaNguoiDung);
                    var data = new ProfileDto
                    {
                        MaBenhNhan = p.MaBenhNhan,
                        HoTen = p.HoTen,
                        Email = p.Email,
                        SoDienThoai = p.SoDienThoai,
                        HinhAnhBenhNhan = p.HinhAnhBenhNhan ?? "default.jpg"
                    };
                    return Ok(new { success = true, message = "Đăng nhập thành công", data = data });
                }
                return Ok(new { success = true, message = "Đăng nhập thành công" });
            }
            return Unauthorized(new { success = false, message = "Sai thông tin đăng nhập" });
        }

        [HttpPost("/api/user/register")]
        public async Task<IActionResult> ApiRegister([FromBody] RegisterRequestDto model)
        {
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == model.Username))
                return BadRequest(new { success = false, message = "Tên đăng nhập đã tồn tại" });

            string otp = new Random().Next(100000, 999999).ToString();
            var reg = new RegistrationModel
            {
                username = model.Username,
                password = model.Password,
                fullname = model.Fullname,
                email = model.Email,
                phone = model.Phone,
                dob = model.Dob,
                gender = model.Gender,
                address = model.Address,
                soBaoHiem = model.SoBaoHiem,
                otp = otp
            };

            HttpContext.Session.SetString("RegistrationData", JsonSerializer.Serialize(reg));
            HttpContext.Session.SetString("RegistrationOtp", otp);

            await SendMailAsync(model.Email, "Mã xác thực đăng ký", $"Mã OTP: {otp}");
            return Ok(new { success = true, message = "Đã gửi OTP" });
        }

        [HttpPost("/api/user/verify-otp")]
        public async Task<IActionResult> ApiVerifyOtp([FromBody] VerifyOtpRequestDto body)
        {
            var sessionOtp = HttpContext.Session.GetString("RegistrationOtp");
            if (body.Otp != sessionOtp) return BadRequest(new { success = false, message = "Mã OTP sai" });

            var json = HttpContext.Session.GetString("RegistrationData");
            if (string.IsNullOrEmpty(json)) return BadRequest(new { success = false, message = "Hết hạn phiên" });

            var reg = JsonSerializer.Deserialize<RegistrationModel>(json);

            // Bắt đầu Transaction
            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new NguoiDung { TenDangNhap = reg.username, VaiTro = "Bệnh nhân", NgayTao = DateTime.Now };
                user.MatKhau = _passwordHasher.HashPassword(user, reg.password);
                _context.NguoiDungs.Add(user);
                await _context.SaveChangesAsync();

                var patient = new BenhNhan
                {
                    MaBenhNhan = user.MaNguoiDung,
                    HoTen = reg.fullname,
                    Email = reg.email,
                    SoDienThoai = reg.phone,
                    NgaySinh = reg.dob,
                    GioiTinh = reg.gender,
                    DiaChi = reg.address,
                    SoBaoHiem = reg.soBaoHiem ?? "",
                    HinhAnhBenhNhan = "default.jpg",
                    NgayTao = DateTime.Now
                };
                _context.BenhNhans.Add(patient);
                await _context.SaveChangesAsync();

                // Tạo ví mặc định
                var wallet = new TaiKhoanBenhNhan
                {
                    MaBenhNhan = user.MaNguoiDung,
                    SoDuHienTai = 0,
                    NgayCapNhatCuoi = DateTime.Now
                };
                _context.TaiKhoanBenhNhan.Add(wallet);
                await _context.SaveChangesAsync();

                await trans.CommitAsync();

                HttpContext.Session.Remove("RegistrationData");
                HttpContext.Session.Remove("RegistrationOtp");

                return Ok(new { success = true, message = "Đăng ký thành công" });
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + msg });
            }
        }

        [HttpPost("/api/user/forgot-password")]
        public async Task<IActionResult> ApiForgotPassword([FromBody] ForgotPasswordRequestDto body)
        {
            if (body.Step == "request_otp")
            {
                var user = await _context.BenhNhans.FirstOrDefaultAsync(u => u.Email == body.Email);
                if (user == null) return BadRequest(new { message = "Email không tồn tại" });

                string otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("ForgotPasswordOtp", otp);
                HttpContext.Session.SetString("ForgotPasswordEmail", body.Email);

                await SendMailAsync(body.Email, "Quên mật khẩu", $"Mã OTP: {otp}");
                return Ok(new { success = true, message = "Đã gửi OTP" });
            }
            else if (body.Step == "verify_otp")
            {
                var sessionOtp = HttpContext.Session.GetString("ForgotPasswordOtp");
                if (body.Otp != sessionOtp) return BadRequest(new { message = "Mã OTP sai" });
                return Ok(new { success = true, message = "OTP đúng" });
            }
            return BadRequest(new { message = "Step không hợp lệ" });
        }

        [HttpPost("/api/user/reset-password")]
        public async Task<IActionResult> ApiResetPassword([FromBody] ResetPasswordRequestDto body)
        {
            var sessionOtp = HttpContext.Session.GetString("ForgotPasswordOtp");
            var email = HttpContext.Session.GetString("ForgotPasswordEmail");

            if (body.Otp != sessionOtp) return BadRequest(new { message = "Mã OTP sai hoặc hết hạn" });
            if (string.IsNullOrEmpty(email)) return BadRequest(new { message = "Phiên hết hạn" });

            var patient = await _context.BenhNhans.FirstOrDefaultAsync(u => u.Email == email);
            var user = await _context.NguoiDungs.FindAsync(patient.MaBenhNhan);

            user.MatKhau = _passwordHasher.HashPassword(user, body.NewPassword);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ForgotPasswordOtp");
            HttpContext.Session.Remove("ForgotPasswordEmail");
            return Ok(new { success = true, message = "Đổi mật khẩu thành công" });
        }

        [HttpPost("/User/SendVerificationCode")]
        public async Task<IActionResult> SendVerificationCode()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Chưa đăng nhập" });

            var patient = await _context.BenhNhans.FindAsync(userId);
            string otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ChangePasswordOtp", otp);
            if (!string.IsNullOrEmpty(patient?.Email))
                await SendMailAsync(patient.Email, "Đổi mật khẩu", $"Mã OTP xác thực: {otp}");

            return Ok(new { success = true });
        }

        [HttpPost("/User/ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto body)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { message = "Chưa đăng nhập" });

            var sessionOtp = HttpContext.Session.GetString("ChangePasswordOtp");
            if (body.verificationCode != sessionOtp) return BadRequest(new { message = "Mã OTP sai" });

            var user = await _context.NguoiDungs.FindAsync(userId);
            var verifyOld = _passwordHasher.VerifyHashedPassword(user, user.MatKhau, body.oldPassword);
            if (verifyOld != PasswordVerificationResult.Success) return BadRequest(new { message = "Mật khẩu cũ sai" });

            user.MatKhau = _passwordHasher.HashPassword(user, body.newPassword);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("ChangePasswordOtp");

            return Ok(new { success = true, message = "Đổi mật khẩu thành công" });
        }

        [HttpGet("/api/user/profile")]
        public async Task<IActionResult> ApiGetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

            var p = await _context.BenhNhans.FindAsync(userId.Value);
            if (p == null) return NotFound();

            // Lấy ví
            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(w => w.MaBenhNhan == userId.Value);
            decimal balance = wallet?.SoDuHienTai ?? 0m;

            var dto = new ProfileDto
            {
                MaBenhNhan = p.MaBenhNhan,
                HoTen = p.HoTen,
                Email = p.Email,
                SoDienThoai = p.SoDienThoai,
                DiaChi = p.DiaChi,
                GioiTinh = p.GioiTinh,
                NgaySinh = p.NgaySinh,
                SoBaoHiem = p.SoBaoHiem,
                HinhAnhBenhNhan = p.HinhAnhBenhNhan,
                SoDu = balance // <--- Trả về số dư
            };
            return Ok(new { success = true, data = dto });
        }

        [HttpGet("/api/user/appointments")]
        public async Task<IActionResult> ApiAppointments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { success = false });

            // Lấy danh sách các ID lịch hẹn đã thanh toán
            var paidIds = await _context.GiaoDichThanhToan
                .Where(g => g.MaBenhNhan == userId.Value && g.LoaiGiaoDich == "Thanh toán lịch hẹn" && g.MaLich != null)
                .Select(g => g.MaLich!.Value)
                .ToListAsync();
            var paidSet = new HashSet<int>(paidIds);

            // Lấy entities rồi ánh xạ ở phía client để an toàn khi dùng paidSet.Contains
            var entities = await _context.LichHens
                .Include(l => l.BacSi)
                .Where(l => l.MaBenhNhan == userId.Value)
                .OrderByDescending(l => l.NgayGio)
                .ToListAsync();

            var list = entities.Select(l => new AppointmentDto
            {
                MaLich = l.MaLich,
                MaBenhNhan = l.MaBenhNhan,
                MaBacSi = l.MaBacSi,
                NgayGio = l.NgayGio,
                BacSiHoTen = l.BacSi != null ? l.BacSi.HoTen : "Không xác định",
                TrangThai = l.TrangThai,
                // Kiểm tra xem ID này có trong danh sách đã trả tiền không
                IsPaid = paidSet.Contains(l.MaLich)
            }).ToList();

            return Ok(new { success = true, data = list });
        }

        // =========================================================
        // PHẦN 4: DTOs & Nested Models
        // =========================================================

        public class RegistrationModel
        {
            public string username { get; set; }
            public string password { get; set; }
            public string fullname { get; set; }
            public DateTime dob { get; set; }
            public string gender { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public string address { get; set; }
            public string soBaoHiem { get; set; }
            public string otp { get; set; }
        }
    }

    // DTOs bên ngoài Controller
    public class RegisterRequestDto { public string Username { get; set; } public string Password { get; set; } public string Fullname { get; set; } public DateTime Dob { get; set; } public string Gender { get; set; } public string Phone { get; set; } public string Email { get; set; } public string Address { get; set; } public string SoBaoHiem { get; set; } }
    public class VerifyOtpRequestDto { public string Otp { get; set; } }
    public class LoginRequestDto { public string Username { get; set; } public string Password { get; set; } }
    public class ForgotPasswordRequestDto { public string Email { get; set; } public string Step { get; set; } public string Otp { get; set; } }
    public class ResetPasswordRequestDto { public string NewPassword { get; set; } public string ConfirmPassword { get; set; } public string Otp { get; set; } }
    public class ChangePasswordDto { public string oldPassword { get; set; } public string newPassword { get; set; } public string confirmPassword { get; set; } public string verificationCode { get; set; } }
    public class ProfileDto { public int MaBenhNhan { get; set; } public string HoTen { get; set; } public DateTime? NgaySinh { get; set; } public string GioiTinh { get; set; } public string SoDienThoai { get; set; } public string Email { get; set; } public string DiaChi { get; set; } public string SoBaoHiem { get; set; } public string HinhAnhBenhNhan { get; set; } public decimal SoDu { get; set; } }
    public class AppointmentDto { public int MaLich { get; set; } public int MaBenhNhan { get; set; } public int MaBacSi { get; set; } public DateTime NgayGio { get; set; } public string BacSiHoTen { get; set; } public string TrangThai { get; set; } public bool IsPaid { get; set; } }
    public class NotificationDto { public int Id { get; set; } public string Title { get; set; } public string Content { get; set; } public DateTime CreatedAt { get; set; } public bool IsRead { get; set; } public int? RelatedAppointmentId { get; set; } }
    public class MarkReadRequest { public int Id { get; set; } }
}