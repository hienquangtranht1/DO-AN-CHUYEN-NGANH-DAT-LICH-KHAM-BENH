using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookinhMVC.Models;
using BookinhMVC.Hubs;          // Để dùng ChatHub
using BookinhMVC.Helpers;       // Để dùng OnlineUserMap
using Microsoft.AspNetCore.SignalR; // Để bắn thông báo
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BookinhMVC.Controllers
{
    public class ChatController : Controller
    {
        private readonly BookingContext _context;
        private readonly IHubContext<ChatHub> _hubContext; // Dùng để bắn tin nhắn từ API

        public ChatController(BookingContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 1. Giao diện Chat Web (Giữ nguyên)
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");
            return View();
        }

        // ======================================================
        // API 1: Lấy danh sách CSKH Online (Cho cả Web & Mobile)
        // ======================================================
        [HttpGet]
        public IActionResult GetListCSKHJson()
        {
            // Lấy danh sách ID đang online từ bộ nhớ (OnlineUserMap)
            // Lưu ý: Cần đảm bảo bạn đã tạo file Helpers/OnlineUserMap.cs như hướng dẫn trước
            var onlineIds = OnlineUserMap.Snapshot().ToHashSet();

            // Chỉ lấy những nhân viên đang Online
            var list = _context.CsKhs
                .Where(c => onlineIds.Contains(c.Id))
                .Select(c => new {
                    id = c.Id,
                    fullName = c.FullName
                })
                .ToList();

            return Json(list);
        }

        // ======================================================
        // API 2: Lấy lịch sử chat (Nâng cấp để hỗ trợ Mobile)
        // ======================================================
        // receiverId: ID của CSKH
        // mobileUserId: ID của khách hàng (Dành cho Mobile gửi lên, Web không cần)
        [HttpGet]
        public async Task<IActionResult> GetHistory(int receiverId, int? mobileUserId)
        {
            // 1. Ưu tiên lấy ID từ Session (Web)
            var myId = HttpContext.Session.GetInt32("UserId");

            // 2. Nếu không có Session (Mobile), lấy từ tham số truyền vào
            if (myId == null && mobileUserId != null)
            {
                myId = mobileUserId;
            }

            if (myId == null) return Unauthorized(new { message = "Chưa đăng nhập" });

            var messages = await _context.ChatMessages
                .Where(m =>
                    // 1. Tin tôi gửi đi
                    (m.SenderId == myId && m.SenderRole == "KhachHang" && m.ReceiverId == receiverId) ||
                    // 2. Tin họ gửi lại
                    (m.SenderId == receiverId && m.SenderRole == "CSKH" && m.ReceiverId == myId)
                )
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    id = m.Id,
                    message = m.Message,
                    isMe = (m.SenderId == myId && m.SenderRole == "KhachHang"),
                    time = m.CreatedAt.ToString("HH:mm"),
                    createdAt = m.CreatedAt
                })
                .ToListAsync();

            return Json(messages);
        }

        // ======================================================
        // API 3: Gửi tin nhắn qua HTTP (Dành cho Mobile App)
        // ======================================================
        // Mobile nên dùng cái này thay vì SignalR trực tiếp để đảm bảo tin nhắn luôn được lưu
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message) || req.SenderId == 0)
                return BadRequest(new { success = false });

            try
            {
                // 1. Lưu vào Database
                var chatMsg = new ChatMessage
                {
                    SenderId = req.SenderId,
                    SenderRole = "KhachHang",
                    ReceiverId = req.ReceiverId, // ID của CSKH
                    Message = req.Message,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.ChatMessages.Add(chatMsg);
                await _context.SaveChangesAsync();

                // 2. Bắn SignalR tới CSKH (để màn hình CSKH hiện tin nhắn ngay)
                // Định dạng ID SignalR của CSKH: "CSKH_{id}"
                string targetSignalRId = $"CSKH_{req.ReceiverId}";
                string mySignalRId = $"KH_{req.SenderId}";

                await _hubContext.Clients.User(targetSignalRId)
                    .SendAsync("ReceiveMessage", mySignalRId, req.Message);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ======================================================
        // API 4: Lấy lịch sử cho CSKH (Giữ nguyên)
        // ======================================================
        [HttpGet]
        [Route("/Chat/GetHistoryForCSKH")]
        public async Task<IActionResult> GetHistoryForCSKH(int customerId)
        {
            var myId = HttpContext.Session.GetInt32("CSKHId");
            if (myId == null) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == myId && m.SenderRole == "CSKH" && m.ReceiverId == customerId) ||
                    (m.SenderId == customerId && m.SenderRole == "KhachHang" && m.ReceiverId == myId)
                )
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    message = m.Message,
                    isMe = (m.SenderId == myId && m.SenderRole == "CSKH"),
                    time = m.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        // Class DTO để nhận dữ liệu từ Mobile
        public class SendMessageRequest
        {
            public int SenderId { get; set; }
            public int ReceiverId { get; set; }
            public string Message { get; set; }
        }
    }
}