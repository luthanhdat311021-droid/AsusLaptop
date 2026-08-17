using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Logic dùng chung để hủy 1 đơn hàng và hoàn lại tồn kho / serial number đã trừ lúc đặt hàng.
    /// Được gọi từ: OrderAutoCancelService (hủy do quá hạn) và các callback thanh toán
    /// VnPayReturn / MomoReturn / MomoIpn (hủy ngay khi thanh toán thất bại / bị hủy).
    /// </summary>
    public static class OrderCancellationHelper
    {
        /// <summary>
        /// Đổi trạng thái đơn thành Cancelled + hoàn kho + hoàn serial number.
        /// KHÔNG tự gọi SaveChangesAsync — caller tự SaveChanges sau khi gọi hàm này
        /// (để có thể gộp chung với các thay đổi khác trong cùng 1 transaction).
        /// </summary>
        public static async Task CancelAndRestoreStockAsync(
            ApplicationDbContext context, Order order, string reason, CancellationToken ct = default)
        {
            // Tránh hoàn kho 2 lần nếu đơn đã bị hủy từ trước (ví dụ IPN và Return cùng xử lý 1 đơn)
            if (order.Status == "Cancelled") return;

            order.Status = "Cancelled";

            var details = await context.OrderDetails
                .Where(d => d.OrderId == order.Id)
                .ToListAsync(ct);

            foreach (var detail in details)
            {
                int stockAfter;

                if (detail.VariantId.HasValue)
                {
                    var variant = await context.ProductVariants.FindAsync(new object?[] { detail.VariantId.Value }, ct);
                    if (variant != null)
                    {
                        variant.Stock += detail.Quantity;
                        stockAfter = variant.Stock;
                    }
                    else stockAfter = 0;
                }
                else
                {
                    var product = await context.Products.FindAsync(new object?[] { detail.ProductId }, ct);
                    if (product != null)
                    {
                        product.Quantity += detail.Quantity;
                        stockAfter = product.Quantity;
                    }
                    else stockAfter = 0;
                }

                context.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = detail.ProductId,
                    VariantId = detail.VariantId,
                    QuantityChange = detail.Quantity,
                    StockAfter = stockAfter,
                    Reason = "Return",
                    Note = $"Hủy đơn #{order.Id}: {reason}",
                    OrderId = order.Id,
                    CreatedAt = DateTime.Now
                });

                var serials = await context.SerialNumbers
                    .Where(s => s.OrderDetailId == detail.Id)
                    .ToListAsync(ct);

                foreach (var serial in serials)
                {
                    serial.Status = "Available";
                    serial.OrderDetailId = null;
                    serial.WarrantyEnd = null;
                    serial.UpdatedAt = DateTime.Now;
                }
            }
        }
    }
}
