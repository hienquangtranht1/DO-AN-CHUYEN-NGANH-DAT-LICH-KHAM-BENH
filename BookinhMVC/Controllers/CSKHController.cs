using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using BookinhMVC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookinhMVC.Controllers
{
    [Route("[controller]/[action]")]
    public class CSKHController : Controller
    {
        private readonly BookingContext _context;

        public CSKHController(BookingContext context)
        {
            _context = context;
        }

        // --- 1. Đăng nhập ---
        [HttpGet]
        [Route("/CSKH/Login")]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("CSKHId") != null) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        [Route("/CSKH/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var cskh = await _context.CsKhs.FirstOrDefaultAsync(c => c.Username == username);
            if (cskh != null && cskh.Password == password) // Thực tế nên mã hóa mật khẩu
            {
                HttpContext.Session.SetInt32("CSKHId", cskh.Id);
                HttpContext.Session.SetString("CSKHName", cskh.FullName);
                HttpContext.Session.SetString("UserRole", "CSKH"); // Đánh dấu Role để Hub nhận biết
                return RedirectToAction("Dashboard");
            }
            ViewData["Error"] = "Tài khoản hoặc mật khẩu không đúng.";
            return View();
        }

        [HttpPost]
        [Route("/CSKH/Logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // --- 2. Dashboard Chính ---
        [HttpGet]
        [Route("/CSKH/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var cskhId = HttpContext.Session.GetInt32("CSKHId");
            if (cskhId == null) return RedirectToAction("Login");

            var cskh = await _context.CsKhs.FindAsync(cskhId);
            return View(cskh);
        }

        // --- 3. API Lấy lịch sử chat (Dành riêng cho CSKH) ---
        [HttpGet]
        [Route("/CSKH/GetHistory")]
        public async Task<IActionResult> GetHistory(int customerId)
        {
            var myId = HttpContext.Session.GetInt32("CSKHId");
            if (myId == null) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m =>
                    // Tin mình gửi đi
                    (m.SenderId == myId && m.SenderRole == "CSKH" && m.ReceiverId == customerId) ||
                    // Tin khách gửi đến
                    (m.SenderId == customerId && m.SenderRole == "KhachHang" && m.ReceiverId == myId)
                )
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    message = m.Message,
                    isMe = (m.SenderId == myId && m.SenderRole == "CSKH"), // Xác định chủ sở hữu
                    time = m.CreatedAt.ToString("HH:mm dd/MM")
                })
                .ToListAsync();

            return Json(messages);
        }
        [HttpGet]
        public IActionResult GetListCSKHJson()
        {
            // Lấy danh sách ID đang online từ OnlineUserMap (thread-safe snapshot)
            var onlineIds = Helpers.OnlineUserMap.Snapshot().ToHashSet();

            // Chỉ lấy nhân viên có trong danh sách Online
            var list = _context.CsKhs
                .Where(c => onlineIds.Contains(c.Id))
                .Select(c => new {
                    id = c.Id,
                    fullName = c.FullName
                })
                .ToList();

            return Json(list);
        }
    }
}