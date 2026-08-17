using Microsoft.AspNetCore.SignalR;

namespace AsusLaptop.Hubs
{
    /// <summary>
    /// Hub đẩy vị trí shipper theo thời gian thực tới khách hàng đang xem
    /// trang theo dõi đơn hàng. Mỗi đơn hàng là một "room" riêng (order-{id})
    /// để tránh gửi nhầm vị trí sang đơn hàng khác.
    /// </summary>
    public class OrderTrackingHub : Hub
    {
        // Khách hàng (hoặc admin) gọi hàm này khi mở trang theo dõi để
        // tham gia đúng "room" của đơn hàng đó.
        public async Task JoinOrderRoom(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(orderId));
        }

        public async Task LeaveOrderRoom(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(orderId));
        }

        public static string GroupName(int orderId) => $"order-{orderId}";
    }
}
