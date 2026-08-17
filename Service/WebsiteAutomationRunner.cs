using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;

namespace AsusLaptop.Services
{
    public class WebsiteAutomationRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifications;
        private readonly FlashSaleEngine _flashEngine;
        private readonly WebsiteAutomationStore _store;
        private readonly ILogger<WebsiteAutomationRunner> _logger;
        private readonly HashSet<int> _alertedProductsToday = new();

        public WebsiteAutomationRunner(
            ApplicationDbContext context,
            NotificationService notifications,
            FlashSaleEngine flashEngine,
            WebsiteAutomationStore store,
            ILogger<WebsiteAutomationRunner> logger)
        {
            _context = context;
            _notifications = notifications;
            _flashEngine = flashEngine;
            _store = store;
            _logger = logger;
        }

        public async Task RunCycleAsync(CancellationToken ct = default)
        {
            var settings = _store.GetSettings();
            int tasksRun = 0;

            if (settings.AutoLowStockAlert)
                tasksRun += await RunLowStockAlertAsync(settings.LowStockThreshold, ct);

            if (settings.AutoCleanStaleCarts)
                tasksRun += await RunCartCleanupAsync(ct);

            if (settings.AutoFlashSaleSync)
                tasksRun += await RunFlashSaleSyncAsync(ct);

            if (settings.AutoMarqueeRotate)
            {
                _store.NextMarqueeIndex(6);
                tasksRun++;
                _store.AddLog("Marquee", "Đã xoay vòng thông báo marquee.");
            }

            _store.RecordRun(_ => { });
            _store.AddLog("Cycle", $"Hoàn tất chu kỳ — {tasksRun} tác vụ.");
            _logger.LogInformation("WebsiteAutomationRunner: cycle done, {Tasks} tasks.", tasksRun);
        }

        private async Task<int> RunLowStockAlertAsync(int threshold, CancellationToken ct)
        {
            var todayKey = DateTime.Now.Date;
            var lowStock = await _context.Products
                .Where(p => p.Quantity > 0 && p.Quantity <= threshold)
                .Select(p => new { p.Id, p.Name, p.Quantity })
                .ToListAsync(ct);

            int alerted = 0;
            foreach (var p in lowStock)
            {
                var cacheKey = p.Id ^ todayKey.GetHashCode();
                if (!_alertedProductsToday.Add(cacheKey)) continue;

                await _notifications.NotifyAdminsAsync(
                    "Cảnh báo tồn kho",
                    $"{p.Name} chỉ còn {p.Quantity} máy.",
                    "Stock",
                    "/Admin/Products");
                alerted++;
            }

            if (alerted > 0)
                _store.AddLog("LowStock", $"Đã cảnh báo {alerted} sản phẩm sắp hết hàng.");

            return alerted > 0 ? 1 : 0;
        }

        private async Task<int> RunCartCleanupAsync(CancellationToken ct)
        {
            var stale = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.Product != null && c.Product.Quantity <= 0)
                .ToListAsync(ct);

            if (!stale.Any()) return 0;

            _context.CartItems.RemoveRange(stale);
            await _context.SaveChangesAsync(ct);
            _store.AddLog("CartCleanup", $"Đã xóa {stale.Count} mục giỏ hàng (sản phẩm hết hàng).");
            return 1;
        }

        private async Task<int> RunFlashSaleSyncAsync(CancellationToken ct)
        {
            var flash = await _flashEngine.BuildAsync(_store.GetSnapshot().FlashSoldOverrides);
            if (!flash.Items.Any()) return 0;

            var ids = flash.Items.Select(i => i.Product.Id).ToList();
            _store.BumpFlashSoldPercents(ids);
            _store.AddLog("FlashSale",
                $"Đồng bộ Flash Sale — {ids.Count} sản phẩm, slot {(flash.IsActiveNow ? "đang chạy" : "chờ")}.");
            return 1;
        }
    }
}
