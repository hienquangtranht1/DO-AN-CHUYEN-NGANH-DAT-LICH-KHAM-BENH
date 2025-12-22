using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookinhMVC.Hubs
{
    public class BookingHub : Hub
    {
        // Hàm này chạy ngay khi App Flutter kết nối tới SignalR
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            // Lấy userId từ đường dẫn kết nối (VD: .../bookingHub?userId=10)
            var userId = httpContext.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                // Đưa kết nối này vào nhóm riêng tên là "User_{userId}"
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                System.Console.WriteLine($"✅ User {userId} đã tham gia vào nhóm SignalR");
            }

            await base.OnConnectedAsync();
        }
    }
}