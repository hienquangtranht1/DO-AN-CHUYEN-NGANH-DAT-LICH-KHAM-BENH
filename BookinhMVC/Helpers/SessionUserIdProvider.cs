using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;

namespace BookinhMVC.Helpers
{
    public class SessionUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            var httpContext = connection.GetHttpContext();

            // --- 1. ƯU TIÊN KIỂM TRA QUERY STRING (DÀNH CHO MOBILE APP) ---
            // Mobile sẽ kết nối dạng: http://.../chatHub?userId=10
            if (httpContext.Request.Query.TryGetValue("userId", out var queryUserId))
            {
                // Định danh Mobile là "KH_{id}"
                return $"KH_{queryUserId}";
            }

            // --- 2. KIỂM TRA SESSION (DÀNH CHO WEB) ---

            // Kiểm tra CSKH
            var cskhId = httpContext.Session.GetInt32("CSKHId");
            var role = httpContext.Session.GetString("UserRole");

            if (cskhId != null && role == "CSKH")
            {
                return $"CSKH_{cskhId}";
            }

            // Kiểm tra Khách hàng trên Web
            var userId = httpContext.Session.GetInt32("UserId");
            if (userId != null && role == "Bệnh nhân")
            {
                return $"KH_{userId}";
            }

            return null; // Không định danh được
        }
    }
}