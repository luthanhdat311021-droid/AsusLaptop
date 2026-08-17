using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    [Authorize(Roles = "Admin,SubAdmin")]
    public class AutomationController : Controller
    {
        private readonly WebsiteAutomationStore _store;
        private readonly FlashSaleEngine _flashEngine;
        private readonly WebsiteAutomationRunner _runner;

        private readonly ApplicationDbContext _context;

        public AutomationController(
            WebsiteAutomationStore store,
            FlashSaleEngine flashEngine,
            WebsiteAutomationRunner runner,
            ApplicationDbContext context)
        {
            _store = store;
            _flashEngine = flashEngine;
            _runner = runner;
            _context = context;
        }

        private bool IsAdminOrSub() =>
            User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));

        /// <summary>API public — frontend poll để đồng bộ Flash Sale / marquee.</summary>
        [AllowAnonymous]
        [HttpGet("/api/automation/live")]
        public async Task<IActionResult> LiveStatus()
        {
            var snap = _store.GetSnapshot();
            var flash = await _flashEngine.BuildAsync(snap.FlashSoldOverrides);

            var marqueePool = new[]
            {
                "Miễn phí vận chuyển đơn từ 500K · ROG Strix — Giảm đến 30%",
                "Trả góp 0% lãi suất 12 tháng · ZenBook OLED siêu mỏng",
                "Bảo hành chính hãng 2 năm · TUF Gaming chuẩn quân đội",
                "Flash Sale 20:00–23:00 mỗi ngày · Săn deal ngay",
                "Laptop Copilot AI — Tư vấn laptop trong 30 giây",
                "Giao nhanh 2 giờ nội thành · Miễn phí đơn từ 500K"
            };

            var dto = new AutomationLiveStatusDto
            {
                ServerTimeIso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                FlashSaleActive = flash.IsActiveNow,
                FlashSaleEndIso = flash.EndTimeIso,
                FlashSlotName = flash.SlotName,
                HeroSlideHint = _store.GetHeroSlideHint(),
                MarqueeMessages = marqueePool.ToList(),
                FlashItems = flash.Items.Select(i => new AutomationLiveFlashItem
                {
                    ProductId = i.Product.Id,
                    SoldPercent = i.SoldPercent
                }).ToList()
            };

            return Json(dto);
        }

        [HttpGet("/Admin/Automation")]
        public IActionResult Index()
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            return View("~/Views/Admin/Automation.cshtml", _store.GetSnapshot());
        }

        [HttpPost("/Admin/Automation/SaveSettings")]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(IFormCollection form)
        {
            if (!IsAdminOrSub()) return Json(new { success = false });

            _store.UpdateSettings(s =>
            {
                s.Enabled = form.ContainsKey("Enabled");
                s.AutoLowStockAlert = form.ContainsKey("AutoLowStockAlert");
                s.AutoCleanStaleCarts = form.ContainsKey("AutoCleanStaleCarts");
                s.AutoFlashSaleSync = form.ContainsKey("AutoFlashSaleSync");
                s.AutoMarqueeRotate = form.ContainsKey("AutoMarqueeRotate");
                if (int.TryParse(form["LowStockThreshold"], out var t)) s.LowStockThreshold = Math.Clamp(t, 1, 50);
                if (int.TryParse(form["CheckIntervalMinutes"], out var m)) s.CheckIntervalMinutes = Math.Clamp(m, 1, 60);
            });

            _store.AddLog("Settings", "Admin cập nhật cấu hình tự động hóa.");
            return Json(new { success = true });
        }

        [HttpPost("/Admin/Automation/RunNow")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunNow()
        {
            if (!IsAdminOrSub()) return Json(new { success = false });

            await _runner.RunCycleAsync();
            return Json(new { success = true, snapshot = _store.GetSnapshot() });
        }

        [HttpPost("/Admin/Automation/SyncMarketPrice")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncMarketPrice()
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền truy cập." });

            var result = await AsusLaptop.Service.MarketPriceAdjustmentEngine.SyncMarketPricesAsync(_context);
            _store.AddLog("MarketPriceSync", $"Đã tự động cập nhật giá thị trường cho {result.TotalProductsUpdated} sản phẩm. Flash Sale được đồng bộ mượt mà.");
            return Json(new { success = true, result });
        }

        [HttpGet("/Admin/Automation/Logs")]
        public IActionResult Logs()
        {
            if (!IsAdminOrSub()) return Json(new { success = false });
            return Json(new { success = true, logs = _store.GetSnapshot().RecentLogs });
        }
    }
}
