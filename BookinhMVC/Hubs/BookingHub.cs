using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookinhMVC.Hubs
{
    public class BookingHub : Hub
    {
        // Hàm này chạy ngay khi App Flutter / web client kết nối tới SignalR
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            // Lấy userId và role từ query string (vd: /bookingHub?userId=10&role=BacSi)
            var userId = httpContext.Request.Query["userId"].ToString();
            var role = httpContext.Request.Query["role"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                // Đưa kết nối này vào nhóm riêng tên là "User_{userId}" (dùng để gửi thông báo cho 1 user)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                System.Console.WriteLine($"✅ User {userId} joined group User_{userId}");
            }

            // Nếu client là bác sĩ, thêm vào nhóm chung "Doctors" để nhận thông báo lịch mới / thay đổi
            if (!string.IsNullOrEmpty(role) && role.Equals("BacSi", System.StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Doctors");
                System.Console.WriteLine($"✅ Connection {Context.ConnectionId} joined group Doctors (role=BacSi)");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext.Request.Query["userId"].ToString();
            var role = httpContext.Request.Query["role"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                System.Console.WriteLine($"ℹ️ User {userId} left group User_{userId}");
            }

            if (!string.IsNullOrEmpty(role) && role.Equals("BacSi", System.StringComparison.OrdinalIgnoreCase))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Doctors");
                System.Console.WriteLine($"ℹ️ Connection {Context.ConnectionId} left group Doctors (role=BacSi)");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}