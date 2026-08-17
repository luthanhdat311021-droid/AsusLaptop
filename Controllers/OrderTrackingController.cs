using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AsusLaptop.Data;
using AsusLaptop.Hubs;
using AsusLaptop.Models;

namespace AsusLaptop.Controllers
{
    /// <summary>
    /// Theo dõi đơn hàng bằng bản đồ thời gian thực (kiểu theo dõi shipper).
    /// - Khách hàng: xem vị trí shipper trực tiếp trên bản đồ (Track).
    /// - Admin/SubAdmin: cập nhật vị trí shipper, gán shipper, ghim điểm giao hàng,
    ///   hoặc chạy mô phỏng di chuyển cho mục đích demo (AdminPanel).
    /// </summary>
    public class OrderTrackingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderTrackingHub> _hub;
        private readonly IServiceScopeFactory _scopeFactory;

        // Toạ độ cửa hàng (điểm xuất phát mặc định) — có thể đổi theo địa chỉ thật
        public const double ShopLat = 10.8231;
        public const double ShopLng = 106.6297; // TP. Hồ Chí Minh

        public OrderTrackingController(ApplicationDbContext context, IHubContext<OrderTrackingHub> hub, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _hub = hub;
            _scopeFactory = scopeFactory;
        }

        private bool IsAdminOrSub() => User.Identity?.IsAuthenticated == true &&
                                        (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));

        // ── KHÁCH HÀNG: xem theo dõi đơn hàng ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account", new { returnUrl = $"/OrderTracking/Track/{id}" });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) { TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!"; return RedirectToAction("MyOrders", "Account"); }

            // Chỉ chủ đơn hàng hoặc admin mới được xem
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (order.UserId != userId && !IsAdminOrSub())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này!";
                return RedirectToAction("MyOrders", "Account");
            }

            ViewBag.ShopLat = ShopLat;
            ViewBag.ShopLng = ShopLng;
            ViewBag.IsAdmin = IsAdminOrSub();
            return View(order);
        }

        // ── ADMIN: bảng điều khiển vị trí shipper cho 1 đơn hàng ────────
        [HttpGet]
        public async Task<IActionResult> AdminPanel(int id)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            ViewBag.ShopLat = ShopLat;
            ViewBag.ShopLng = ShopLng;
            return View(order);
        }

        // ── ADMIN: gán shipper phụ trách đơn hàng ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignShipper(int orderId, string shipperName, string shipperPhone)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            order.ShipperName = shipperName;
            order.ShipperPhone = shipperPhone;
            // Vị trí xuất phát mặc định là kho/cửa hàng nếu chưa có vị trí nào
            if (order.ShipperLat == null || order.ShipperLng == null)
            {
                order.ShipperLat = ShopLat;
                order.ShipperLng = ShopLng;
            }
            order.LastLocationUpdate = DateTime.Now;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group(OrderTrackingHub.GroupName(orderId)).SendAsync("ReceiveShipperInfo", new
            {
                shipperName = order.ShipperName,
                shipperPhone = order.ShipperPhone,
                lat = order.ShipperLat,
                lng = order.ShipperLng
            });

            return Json(new { success = true });
        }

        // ── ADMIN: ghim toạ độ điểm giao hàng (khách hàng) trên bản đồ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDestination(int orderId, double lat, double lng)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            order.DestinationLat = lat;
            order.DestinationLng = lng;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group(OrderTrackingHub.GroupName(orderId)).SendAsync("ReceiveDestination", new { lat, lng });
            return Json(new { success = true });
        }

        // ── ADMIN: cập nhật vị trí shipper (gọi liên tục từ app/thiết bị shipper) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLocation(int orderId, double lat, double lng)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            order.ShipperLat = lat;
            order.ShipperLng = lng;
            order.LastLocationUpdate = DateTime.Now;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group(OrderTrackingHub.GroupName(orderId)).SendAsync("ReceiveLocation", new
            {
                lat,
                lng,
                updatedAt = order.LastLocationUpdate
            });

            return Json(new { success = true });
        }

        // ── ADMIN (DEMO): mô phỏng shipper di chuyển dần từ cửa hàng
        //    tới điểm giao hàng, đẩy vị trí realtime qua SignalR mỗi giây.
        //    Dùng khi chưa có thiết bị GPS thật của shipper.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateMovement(int orderId, int steps = 30, int intervalMs = 1500)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            if (order.DestinationLat == null || order.DestinationLng == null)
                return Json(new { success = false, message = "Chưa ghim điểm giao hàng trên bản đồ!" });

            double startLat = order.ShipperLat ?? ShopLat;
            double startLng = order.ShipperLng ?? ShopLng;
            double endLat = order.DestinationLat.Value;
            double endLng = order.DestinationLng.Value;
            int totalSteps = Math.Max(steps, 2);

            // Chạy nền, không chặn response — admin bấm nút xong có thể rời trang
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                for (int i = 1; i <= totalSteps; i++)
                {
                    double t = (double)i / totalSteps;
                    double curLat = startLat + (endLat - startLat) * t;
                    double curLng = startLng + (endLng - startLng) * t;

                    var o = await db.Orders.FindAsync(orderId);
                    if (o == null) break;
                    o.ShipperLat = curLat;
                    o.ShipperLng = curLng;
                    o.LastLocationUpdate = DateTime.Now;
                    await db.SaveChangesAsync();

                    await _hub.Clients.Group(OrderTrackingHub.GroupName(orderId)).SendAsync("ReceiveLocation", new
                    {
                        lat = curLat,
                        lng = curLng,
                        updatedAt = o.LastLocationUpdate
                    });

                    await Task.Delay(intervalMs);
                }

                // Khi tới nơi, tự động thông báo hoàn tất chặng đường (không đổi trạng thái đơn hàng)
                await _hub.Clients.Group(OrderTrackingHub.GroupName(orderId)).SendAsync("ArrivedAtDestination");
            });

            return Json(new { success = true, message = "Đã bắt đầu mô phỏng di chuyển!" });
        }
    }
}
