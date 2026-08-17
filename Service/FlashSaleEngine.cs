using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    public class FlashSaleEngine
    {
        private readonly ApplicationDbContext _context;

        public FlashSaleEngine(ApplicationDbContext context)
        {
            _context = context;
        }

        private static DateTime GetVietnamTime()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(7);
            }
        }

        public async Task<FlashSaleSectionViewModel> BuildAsync(Dictionary<int, int>? soldOverrides = null)
        {
            var now = GetVietnamTime();
            var startSlot = new TimeSpan(20, 0, 0);
            var endSlot = new TimeSpan(23, 0, 0);

            bool isActiveNow = now.TimeOfDay >= startSlot && now.TimeOfDay < endSlot;
            DateTime endTime;

            if (isActiveNow)
                endTime = now.Date.Add(endSlot);
            else if (now.TimeOfDay < startSlot)
                endTime = now.Date.Add(startSlot);
            else
                endTime = now.Date.AddDays(1).Add(startSlot);

            int seed = now.Year * 10000 + now.Month * 100 + now.Day + (isActiveNow ? 7 : 0);
            var rand = new Random(seed);

            var allProdsForSale = await _context.Products.OrderByDescending(p => p.ViewCount).ToListAsync();
            var flashSaleProds = allProdsForSale.OrderBy(_ => rand.Next()).Take(4).ToList();
            var discounts = new[] { 5, 8, 10, 12, 15, 18, 20 };

            var flashItems = flashSaleProds.Select((p, idx) =>
            {
                int discPercent = discounts[(seed + idx) % discounts.Length];
                decimal currentPrice = p.Price;
                decimal displayOrigP = p.OriginalPrice > currentPrice ? p.OriginalPrice : Math.Round(currentPrice * 1.15m / 100000m) * 100000m;
                
                // Flash sale price is calculated dynamically on the latest base market price
                decimal flashP = Math.Round((currentPrice * (100 - discPercent) / 100) / 100000m) * 100000m;

                if (flashP >= currentPrice)
                    flashP = Math.Max(500000m, currentPrice - 500000m);

                int totalDiscPercent = displayOrigP > 0
                    ? (int)Math.Round((1.0m - (flashP / displayOrigP)) * 100.0m)
                    : discPercent;

                int soldPercent = 65 + (seed + idx * 11) % 30;
                if (soldOverrides != null && soldOverrides.TryGetValue(p.Id, out var overrideVal))
                    soldPercent = Math.Min(98, overrideVal);

                return new FlashSaleItemViewModel
                {
                    Product = p,
                    DiscountPercent = Math.Max(5, totalDiscPercent),
                    OriginalPrice = displayOrigP,
                    FlashPrice = flashP,
                    SoldPercent = soldPercent
                };
            }).ToList();

            return new FlashSaleSectionViewModel
            {
                IsActiveNow = isActiveNow,
                SlotName = "20:00 - 23:00",
                EndTimeIso = endTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                Items = flashItems
            };
        }
    }
}
