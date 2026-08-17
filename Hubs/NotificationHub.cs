using Microsoft.AspNetCore.SignalR;

namespace AsusLaptop.Hubs
{
    /// <summary>
    /// Đẩy thông báo (chuông thông báo) theo thời gian thực.
    /// Mỗi user có 1 group riêng "user-{id}"; Admin/SubAdmin còn tham gia group "admins"
    /// để nhận thông báo broadcast (VD: có đơn hàng mới).
    /// </summary>
    public class NotificationHub : Hub
    {
        public const string AdminGroup = "admins";
        public static string UserGroup(int userId) => $"user-{userId}";

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = Context.User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
                }

                if (Context.User.IsInRole("Admin") || Context.User.IsInRole("SubAdmin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
                }
            }
            await base.OnConnectedAsync();
        }
    }
}
