using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using BookinhMVC.Helpers; // Nơi chứa OnlineUserMap
using System.Linq;

namespace BookinhMVC.Hubs
{
    public class ChatHub : Hub
    {
        private readonly BookingContext _context;

        public ChatHub(BookingContext context)
        {
            _context = context;
        }

        // ========================================================================
        // 1. QUẢN LÝ KẾT NỐI
        // ========================================================================
        public override async Task OnConnectedAsync()
        {
            // Context.UserIdentifier được cung cấp bởi SessionUserIdProvider
            // Nó sẽ có dạng "CSKH_1" hoặc "KH_10"
            string userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                // Nếu là CSKH -> Thêm vào danh sách Online
                if (userId.StartsWith("CSKH_"))
                {
                    int cskhId = int.Parse(userId.Split('_')[1]);
                    OnlineUserMap.Add(cskhId);

                    // Báo cho tất cả (Web & Mobile) biết danh sách online vừa thay đổi
                    // Mobile sẽ nghe sự kiện này để gọi API tải lại danh sách
                    await Clients.All.SendAsync("OnlineListChanged");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && userId.StartsWith("CSKH_"))
            {
                int cskhId = int.Parse(userId.Split('_')[1]);

                // Xóa khỏi danh sách Online
                OnlineUserMap.Remove(cskhId);

                // Báo cập nhật
                await Clients.All.SendAsync("OnlineListChanged");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ========================================================================
        // 2. GỬI TIN NHẮN (LƯU DB + GỬI REALTIME)
        // ========================================================================
        public async Task SendMessage(string receiverSignalRId, string message)
        {
            string senderSignalRId = Context.UserIdentifier; // VD: "KH_10"

            if (string.IsNullOrEmpty(receiverSignalRId) || string.IsNullOrEmpty(message)) return;

            try
            {
                // 1. Phân tích ID người gửi
                var senderInfo = ParseSignalRId(senderSignalRId);
                var receiverInfo = ParseSignalRId(receiverSignalRId);

                if (senderInfo.Id > 0 && receiverInfo.Id > 0)
                {
                    // 2. Lưu vào Database
                    var chatMsg = new ChatMessage
                    {
                        SenderId = senderInfo.Id,
                        SenderRole = senderInfo.Role, // "CSKH" hoặc "KhachHang"
                        ReceiverId = receiverInfo.Id,
                        Message = message,
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };
                    _context.ChatMessages.Add(chatMsg);
                    await _context.SaveChangesAsync();

                    // 3. Gửi Realtime tới người nhận
                    await Clients.User(receiverSignalRId).SendAsync("ReceiveMessage", senderSignalRId, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi tin nhắn: {ex.Message}");
            }
        }

        // ========================================================================
        // 3. QUY TRÌNH KẾT NỐI (REQUEST - ACCEPT - REJECT)
        // ========================================================================

        // Mobile/Web Khách hàng gửi yêu cầu tới CSKH
        public async Task RequestChat(int targetCskhId)
        {
            string senderSignalRId = Context.UserIdentifier; // "KH_10"

            // Lấy tên hiển thị:
            // Với Web: Có thể lấy từ Session.
            // Với Mobile: Session null, nên tạm thời dùng ID hoặc hiển thị "Khách hàng".
            // Để tốt nhất: Mobile nên gửi tên qua tham số, hoặc CSKH tự tra cứu ID trong DB.
            string senderName = GetCallerName();

            var senderInfo = ParseSignalRId(senderSignalRId);

            // Gửi sự kiện tới CSKH cụ thể
            await Clients.User($"CSKH_{targetCskhId}").SendAsync("ReceiveChatRequest", senderSignalRId, senderName, senderInfo.Id);
        }

        // CSKH chấp nhận yêu cầu
        public async Task AcceptChat(string customerSignalRId)
        {
            await Clients.User(customerSignalRId).SendAsync("ChatAccepted");
        }

        // CSKH từ chối
        public async Task RejectChat(string customerSignalRId)
        {
            await Clients.User(customerSignalRId).SendAsync("ConnectionDenied");
        }

        // Khách hàng hủy yêu cầu
        public async Task CancelRequest(int cskhId)
        {
            string senderSignalRId = Context.UserIdentifier;
            var senderInfo = ParseSignalRId(senderSignalRId);

            await Clients.User($"CSKH_{cskhId}").SendAsync("ClientCancelled", senderInfo.Id);
        }

        // ========================================================================
        // 4. HELPER METHODS
        // ========================================================================

        // Tách chuỗi "KH_10" thành {Role="KhachHang", Id=10}
        private (string Role, int Id) ParseSignalRId(string signalRId)
        {
            if (string.IsNullOrEmpty(signalRId)) return ("", 0);

            var parts = signalRId.Split('_');
            if (parts.Length < 2) return ("", 0);

            string prefix = parts[0];
            if (int.TryParse(parts[1], out int id))
            {
                string role = prefix == "CSKH" ? "CSKH" : "KhachHang";
                return (role, id);
            }
            return ("", 0);
        }

        // Lấy tên người gọi (Hỗ trợ cả Web Session và Default)
        private string GetCallerName()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext?.Session != null)
            {
                var name = httpContext.Session.GetString("PatientName");
                if (!string.IsNullOrEmpty(name)) return name;
            }

            // Nếu là Mobile (không có session), trả về ID để CSKH nhận diện tạm
            string id = Context.UserIdentifier;
            return id.StartsWith("KH_") ? $"Khách hàng ({id.Split('_')[1]})" : "Khách hàng";
        }
    }
}