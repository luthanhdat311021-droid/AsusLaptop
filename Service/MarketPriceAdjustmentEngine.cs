using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Service
{
    public class PriceAdjustmentLog
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public double AdjustmentPercent { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class MarketSyncResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public int TotalProductsUpdated { get; set; }
        public List<PriceAdjustmentLog> Logs { get; set; } = new List<PriceAdjustmentLog>();
        public DateTime LastSyncedAt { get; set; } = DateTime.Now;
    }

    public static class MarketPriceAdjustmentEngine
    {
        private static readonly string[] ReasonsIncrease = new[]
        {
            "Biến động tỷ giá USD/VND (+1.8%)",
            "Tăng chi phí chip bán dẫn & RAM toàn cầu",
            "Chi phí vận chuyển logistics hàng hải tăng nhẹ"
        };

        private static readonly string[] ReasonsDecrease = new[]
        {
            "Tối ưu hóa chuỗi cung ứng linh kiện OLED",
            "Chương trình trợ giá từ Asus Global",
            "Giảm chi phí sản xuất bo mạch thế hệ mới"
        };

        public static async Task<MarketSyncResult> SyncMarketPricesAsync(ApplicationDbContext context)
        {
            var products = await context.Products.ToListAsync();
            var logs = new List<PriceAdjustmentLog>();
            var rand = new Random();

            foreach (var p in products)
            {
                // Market fluctuation index: between -4.5% and +3.5%
                double deltaPercent = Math.Round((rand.NextDouble() * 8.0 - 4.5), 1);
                if (Math.Abs(deltaPercent) < 0.8) continue; // Skip tiny changes

                decimal oldP = p.Price;
                decimal factor = 1.0m + ((decimal)deltaPercent / 100.0m);
                decimal newP = Math.Round((oldP * factor) / 100000m) * 100000m;

                if (newP == oldP || newP <= 1000000m) continue;

                p.Price = newP;
                if (p.OriginalPrice < p.Price)
                {
                    p.OriginalPrice = Math.Round(p.Price * 1.15m / 100000m) * 100000m;
                }

                string reason = deltaPercent > 0
                    ? ReasonsIncrease[rand.Next(ReasonsIncrease.Length)]
                    : ReasonsDecrease[rand.Next(ReasonsDecrease.Length)];

                logs.Add(new PriceAdjustmentLog
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    OldPrice = oldP,
                    NewPrice = newP,
                    AdjustmentPercent = deltaPercent,
                    Reason = reason,
                    Timestamp = DateTime.Now
                });
            }

            if (logs.Any())
            {
                await context.SaveChangesAsync();
            }

            return new MarketSyncResult
            {
                Success = true,
                Message = $"Đã tự động điều chỉnh giá thị trường thành công cho {logs.Count} sản phẩm!",
                TotalProductsUpdated = logs.Count,
                Logs = logs,
                LastSyncedAt = DateTime.Now
            };
        }
    }
}
