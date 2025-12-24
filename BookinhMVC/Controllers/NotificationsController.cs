using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using BookinhMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace BookinhMVC.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly BookingContext _db;

        public NotificationsController(BookingContext db)
        {
            _db = db;
        }

        // GET: api/notifications/list?take=10
        [HttpGet("list")]
        public async Task<IActionResult> List(int take = 20)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var list = await _db.ThongBaos
                .Where(t => t.MaNguoiDung == userId.Value)
                .OrderByDescending(t => t.NgayTao)
                .Take(take)
                .Select(t => new {
                    id = t.MaThongBao,
                    title = t.TieuDe,
                    content = t.NoiDung,
                    createdAt = t.NgayTao,
                    isRead = t.DaXem,
                    appointmentId = t.MaLichHen
                })
                .ToListAsync();

            return Ok(new { success = true, data = list });
        }

        // GET: api/notifications/count
        [HttpGet("count")]
        public async Task<IActionResult> Count()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Ok(new { success = false, unread = 0 });

            var unread = await _db.ThongBaos.CountAsync(t => t.MaNguoiDung == userId.Value && !t.DaXem);
            return Ok(new { success = true, unread });
        }

        // POST: api/notifications/markread
        [HttpPost("markread")]
        public async Task<IActionResult> MarkRead([FromBody] int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var t = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == id && x.MaNguoiDung == userId.Value);
            if (t == null) return NotFound();

            t.DaXem = true;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST: api/notifications/markall
        [HttpPost("markall")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var list = await _db.ThongBaos.Where(t => t.MaNguoiDung == userId.Value && !t.DaXem).ToListAsync();
            if (list.Any())
            {
                foreach (var t in list) t.DaXem = true;
                await _db.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }
    }
}