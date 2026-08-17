using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Chạy nền liên tục: tự động hủy các đơn hàng thanh toán online (VNPay/Momo/BankTransfer)
    /// bị "treo" quá lâu ở trạng thái chờ thanh toán (ví dụ khách hủy giữa chừng, phiên hết hạn,
    /// đóng tab trình duyệt...), đồng thời hoàn lại tồn kho và serial number đã trừ lúc đặt hàng.
    /// </summary>
    public class OrderAutoCancelService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderAutoCancelService> _logger;

        // ── Cấu hình: sau bao nhiêu phút chưa thanh toán thì tự hủy ─────────────
        private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(15);

        // ── Cấu hình: bao lâu quét lại 1 lần ─────────────────────────────────────
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        private static readonly string[] OnlinePaymentMethods = { "VNPay", "Momo", "BankTransfer" };

        public OrderAutoCancelService(IServiceScopeFactory scopeFactory, ILogger<OrderAutoCancelService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelStaleOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OrderAutoCancelService: lỗi khi quét đơn hàng treo.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CancelStaleOrdersAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expireBefore = DateTime.Now - PaymentTimeout;

            // Đơn hàng thanh toán online, chưa thanh toán xong, đã quá hạn chờ
            var staleOrders = await context.Orders
                .Where(o => OnlinePaymentMethods.Contains(o.PaymentMethod)
                            && o.PaymentStatus == "Pending"
                            && o.Status != "Cancelled"
                            && o.OrderDate < expireBefore)
                .ToListAsync(ct);

            if (!staleOrders.Any()) return;

            foreach (var order in staleOrders)
            {
                await OrderCancellationHelper.CancelAndRestoreStockAsync(
                    context, order, $"quá hạn thanh toán ({PaymentTimeout.TotalMinutes} phút)", ct);
                order.PaymentStatus = "Expired";
            }

            await context.SaveChangesAsync(ct);

            _logger.LogInformation("OrderAutoCancelService: đã tự hủy {Count} đơn hàng quá hạn thanh toán.", staleOrders.Count);
        }
    }
}
