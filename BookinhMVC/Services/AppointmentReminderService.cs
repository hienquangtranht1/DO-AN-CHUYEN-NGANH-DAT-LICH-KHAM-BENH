using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR; // Import SignalR
using BookinhMVC.Models;
using BookinhMVC.Hubs; // Import Hub của bạn
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BookinhMVC.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<BookingHub> _hubContext; // Inject HubContext để gửi SignalR

        // Inject IHubContext vào Constructor
        public AppointmentReminderService(IServiceProvider serviceProvider, IHubContext<BookingHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendReminders();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [Background Service Error]: {ex.Message}");
                }

                // Quét định kỳ mỗi 15 phút (hoặc 5 phút tùy nhu cầu)
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task CheckAndSendReminders()
        {
            // Tạo scope mới để lấy DbContext (vì BackgroundService là Singleton còn DbContext là Scoped)
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BookingContext>();

                var now = DateTime.Now;
                var tomorrow = now.AddHours(24);

                // Lấy danh sách lịch hẹn cần thông báo:
                // 1. Thời gian > hiện tại & <= 24h tới
                // 2. Trạng thái "Đã xác nhận"
                // 3. Chưa được thông báo (DaThongBao == false)
                var appointments = await context.LichHens
                    .Include(l => l.BenhNhan)
                    .Include(l => l.BacSi)
                    .Where(l => l.NgayGio > now
                                && l.NgayGio <= tomorrow
                                && l.TrangThai == "Đã xác nhận"
                                && !l.DaThongBao)
                    .ToListAsync();

                if (appointments.Any())
                {
                    Console.WriteLine($"🔔 Tìm thấy {appointments.Count} lịch hẹn ĐÃ XÁC NHẬN cần nhắc nhở.");
                }

                foreach (var appt in appointments)
                {
                    try
                    {
                        // 1. Gửi Email nhắc nhở
                        string subject = $"[NHẮC NHỞ] Lịch khám ngày mai: {appt.NgayGio:dd/MM/yyyy HH:mm}";
                        string body = $"Chào {appt.BenhNhan.HoTen},\n\n" +
                                      $"Hệ thống nhắc bạn có lịch khám ĐÃ XÁC NHẬN với BS {appt.BacSi.HoTen}.\n" +
                                      $"Thời gian: {appt.NgayGio:HH:mm} ngày {appt.NgayGio:dd/MM/yyyy}.\n" +
                                      $"Vui lòng đến đúng giờ.";

                        await SendEmailBackground(appt.BenhNhan.Email, subject, body);

                        // 2. Lưu thông báo vào Database (để hiển thị trong danh sách thông báo của App)
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = appt.MaBenhNhan,
                            TieuDe = "Nhắc nhở lịch hẹn",
                            NoiDung = $"Ngày mai {appt.NgayGio:dd/MM HH:mm} bạn có lịch khám với BS {appt.BacSi.HoTen}.",
                            NgayTao = DateTime.Now,
                            DaXem = false,
                            MaLichHen = appt.MaLich
                        };
                        context.ThongBaos.Add(thongBao);

                        // 3. 🚀 GỬI SIGNALR REAL-TIME CHO MOBILE APP
                        // Gửi đến Group User cụ thể (được tạo trong Hub khi user kết nối)
                        await _hubContext.Clients.Group($"User_{appt.MaBenhNhan}").SendAsync("ReceiveNotification", new
                        {
                            maThongBao = 0, // ID tạm (0 vì chưa SaveChanges, App Mobile có thể bỏ qua hoặc dùng ID giả)
                            tieuDe = thongBao.TieuDe,
                            noiDung = thongBao.NoiDung,
                            ngayTao = thongBao.NgayTao,
                            maLichHen = appt.MaLich
                        });

                        Console.WriteLine($"✅ Sent SignalR notification to User_{appt.MaBenhNhan}");

                        // 4. Đánh dấu là ĐÃ THÔNG BÁO để không quét lại lần sau
                        appt.DaThongBao = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Err sending reminder for Appt #{appt.MaLich}: {ex.Message}");
                    }
                }

                // Lưu tất cả thay đổi (cập nhật DaThongBao = true và thêm ThongBao mới) xuống DB
                await context.SaveChangesAsync();
            }
        }

        private async Task SendEmailBackground(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("FOUR ROCK HOSPITAL", "hienquangtranht1@gmail.com"));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();
                // Lưu ý: Cấu hình SMTP nên được lấy từ appsettings.json thay vì hardcode
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync("hienquangtranht1@gmail.com", "aigh nsyp dgyu emhc");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email failed: {ex.Message}");
            }
        }
    }
}