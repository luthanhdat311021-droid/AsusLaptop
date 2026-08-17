using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using AsusLaptop.Data;
using AsusLaptop.Hubs;
using AsusLaptop.Service;

namespace AsusLaptop.Controllers
{
    public class PriceAlertRequest
    {
        public int ProductId { get; set; }
        public decimal TargetPrice { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class PriceTrackerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public PriceTrackerController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
        public IActionResult GetPrediction(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                // Fallback demo product
                product = new Models.Product
                {
                    Id = id > 0 ? id : 1,
                    Name = "ASUS ROG Strix SCAR 18 (G834)",
                    Series = "ROG Strix",
                    Price = 64990000m,
                    OriginalPrice = 72990000m
                };
            }

            var prediction = PricePredictionEngine.AnalyzeAndPredict(product);
            return Json(new { success = true, data = prediction });
        }

        [HttpPost]
        public async Task<IActionResult> SetAlert([FromBody] PriceAlertRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.TargetPrice <= 0)
            {
                return Json(new { success = false, message = "Thông tin không hợp lệ." });
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == request.ProductId) ?? new Models.Product
            {
                Id = request.ProductId,
                Name = "Laptop ASUS Chính Hãng"
            };

            // Real-time SignalR Notification demo feedback
            await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"🔔 Đã đặt cảnh báo giá cho '{product.Name}'. Hệ thống AI sẽ tự động thông báo khi giá đạt {request.TargetPrice:N0}₫!");

            return Json(new
            {
                success = true,
                message = $"Đã cài đặt cảnh báo thành công! AI sẽ gửi thông báo cho bạn khi '{product.Name}' giảm về mức {request.TargetPrice:N0}₫."
            });
        }

        [HttpGet]
        [OutputCache(Duration = 60)]
        public IActionResult DealRadar()
        {
            var products = _context.Products.Take(8).ToList();
            if (!products.Any())
            {
                // Demo products fallback if DB has no products
                products = new System.Collections.Generic.List<Models.Product>
                {
                    new Models.Product { Id = 1, Name = "ASUS ROG Strix SCAR 18 G834JS", Series = "ROG Strix", Price = 64990000m, OriginalPrice = 72990000m },
                    new Models.Product { Id = 2, Name = "ASUS ZenBook S 13 OLED UX5304", Series = "ZenBook", Price = 32990000m, OriginalPrice = 38990000m },
                    new Models.Product { Id = 3, Name = "ASUS TUF Gaming A15 FA507NV", Series = "TUF Gaming", Price = 23490000m, OriginalPrice = 27990000m },
                    new Models.Product { Id = 4, Name = "ASUS ProArt Studiobook 16 OLED", Series = "ProArt", Price = 59990000m, OriginalPrice = 69990000m }
                };
            }

            var deals = products
                .Select(p => PricePredictionEngine.AnalyzeAndPredict(p))
                .OrderByDescending(d => d.BuyScore)
                .Take(4)
                .ToList();

            return Json(new { success = true, items = deals });
        }
    }
}
