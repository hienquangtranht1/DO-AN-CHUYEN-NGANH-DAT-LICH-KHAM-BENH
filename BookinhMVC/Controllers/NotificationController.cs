using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BookinhMVC.Models;
using BookinhMVC.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookinhMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly BookingContext _context;
        private readonly IHubContext<BookingHub> _hubContext;

        public NotificationController(BookingContext context, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ==========================================================
        // 1. LẤY DANH SÁCH & SỐ LƯỢNG (GET) - ĐÃ TỐI ƯU
        // ==========================================================

        // API: /api/Notification/GetUserNotifications?userId=1&page=1&pageSize=20
        [HttpGet("GetUserNotifications")]
        public async Task<IActionResult> GetUserNotifications(int userId, int page = 1, int pageSize = 20)
        {
            try
            {
                // 🔥 QUAN TRỌNG: Thêm .AsNoTracking() để đọc nhanh hơn và không khóa DB
                var query = _context.ThongBaos.AsNoTracking()
                    .Where(n => n.MaNguoiDung == userId)
                    .OrderByDescending(n => n.NgayTao);

                var totalCount = await query.CountAsync();

                var notifs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.MaThongBao,
                        n.TieuDe,
                        n.NoiDung,
                        n.NgayTao,
                        n.DaXem,
                        n.MaLichHen
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifs,
                    total = totalCount,
                    page = page,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // API: /api/Notification/UnreadCount?userId=1
        [HttpGet("UnreadCount")]
        public async Task<IActionResult> UnreadCount(int userId)
        {
            try
            {
                // 🔥 QUAN TRỌNG: Thêm .AsNoTracking()
                var count = await _context.ThongBaos.AsNoTracking()
                                .CountAsync(n => n.MaNguoiDung == userId && !n.DaXem);

                return Ok(new { success = true, count = count });
            }
            catch
            {
                // Nếu lỗi thì trả về 0 để App không bị crash
                return Ok(new { success = false, count = 0 });
            }
        }

        // ==========================================================
        // 2. TƯƠNG TÁC (POST/DELETE)
        // ==========================================================

        // API: /api/Notification/MarkAsRead?id=10
        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _context.ThongBaos.FindAsync(id);
            if (notif != null && !notif.DaXem)
            {
                notif.DaXem = true;
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        // API: /api/Notification/MarkAllAsRead?userId=1
        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            var list = await _context.ThongBaos
                .Where(n => n.MaNguoiDung == userId && !n.DaXem)
                .ToListAsync();

            if (list.Any())
            {
                foreach (var item in list) item.DaXem = true;
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        // API: /api/Notification/Delete?id=10
        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ThongBaos.FindAsync(id);
            if (item != null)
            {
                _context.ThongBaos.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        // ==========================================================
        // 3. REAL-TIME (Gửi thông báo từ Admin/Bác sĩ)
        // ==========================================================

        // API: /api/Notification/SendRealtime
        [HttpPost("SendRealtime")]
        public async Task<IActionResult> SendRealtime(int userId, string title, string message, int? maLichHen = null)
        {
            try
            {
                // 1. Lưu vào Database
                var thongBao = new ThongBao
                {
                    MaNguoiDung = userId,
                    TieuDe = title,
                    NoiDung = message,
                    NgayTao = DateTime.Now,
                    DaXem = false,
                    MaLichHen = maLichHen
                };
                _context.ThongBaos.Add(thongBao);
                await _context.SaveChangesAsync();

                // 2. Gửi SignalR tới User đó
                // Lưu ý: Đảm bảo bên Flutter user đã join group có tên "User_{userId}" chưa?
                // Nếu chưa join group thì dùng .Clients.All (để test) hoặc check lại logic join group ở Hub.
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
                {
                    maThongBao = thongBao.MaThongBao,
                    tieuDe = title,
                    noiDung = message,
                    ngayTao = thongBao.NgayTao,
                    maLichHen = maLichHen
                });

                return Ok(new { success = true, message = "Đã gửi thông báo Realtime" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}