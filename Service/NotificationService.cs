using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Hubs;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Tạo và đẩy thông báo (chuông thông báo) tới người dùng / admin theo thời gian thực
    /// qua SignalR, đồng thời lưu lại DB để hiển thị khi tải lại trang.
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        private async Task PushAsync(Notification n)
        {
            var payload = new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                actionUrl = n.ActionUrl,
                createdAt = n.CreatedAt
            };

            if (n.UserId.HasValue)
                await _hub.Clients.Group(NotificationHub.UserGroup(n.UserId.Value)).SendAsync("ReceiveNotification", payload);
            else
                await _hub.Clients.Group(NotificationHub.AdminGroup).SendAsync("ReceiveNotification", payload);
        }

        /// <summary>Gửi thông báo cho 1 người dùng cụ thể (VD: cập nhật đơn hàng).</summary>
        public async Task NotifyUserAsync(int userId, string title, string message, string type = "System", string? actionUrl = null)
        {
            var n = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();
            await PushAsync(n);
        }

        /// <summary>Gửi thông báo tới tất cả Admin/SubAdmin (VD: có đơn hàng mới).</summary>
        public async Task NotifyAdminsAsync(string title, string message, string type = "System", string? actionUrl = null)
        {
            var n = new Notification
            {
                UserId = null,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();
            await PushAsync(n);
        }
    }
}
