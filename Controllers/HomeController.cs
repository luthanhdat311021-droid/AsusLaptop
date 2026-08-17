using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;
using System.Security.Claims;

namespace AsusLaptop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly PersonalizedRecommendationService _recommendationService;
        private readonly FlashSaleEngine _flashEngine;
        private readonly WebsiteAutomationStore _automationStore;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger,
            PersonalizedRecommendationService recommendationService,
            FlashSaleEngine flashEngine,
            WebsiteAutomationStore automationStore)
        {
            _context = context;
            _logger = logger;
            _recommendationService = recommendationService;
            _flashEngine = flashEngine;
            _automationStore = automationStore;
        }

        /// <summary>
        /// Sitemap XML động — liệt kê tất cả sản phẩm + các trang tĩnh quan trọng.
        /// Truy cập tại: /sitemap.xml
        /// </summary>
        [Route("sitemap.xml")]
        public async Task<IActionResult> Sitemap()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = await _context.Products
                .Select(p => new { p.Id })
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            void AddUrl(string path, string changefreq, string priority)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
                sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                sb.AppendLine("  </url>");
            }

            // Trang tĩnh quan trọng
            AddUrl("/", "daily", "1.0");
            AddUrl("/Support/HuongDanMuaHang", "monthly", "0.5");
            AddUrl("/Support/ChinhSachDoiTra", "monthly", "0.5");
            AddUrl("/Support/PhuongThucThanhToan", "monthly", "0.5");
            AddUrl("/Support/CauHoiThuongGap", "monthly", "0.5");
            AddUrl("/Support/TraCuuBaoHanh", "monthly", "0.5");

            // Từng trang chi tiết sản phẩm
            foreach (var p in products)
            {
                AddUrl($"/Product/Details/{p.Id}", "weekly", "0.8");
            }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", System.Text.Encoding.UTF8);
        }

        [OutputCache(PolicyName = "CatalogCache")]
        public async Task<IActionResult> Index(
            string? search = null,
            string? series = null,
            string? brand = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null,
            int page = 1)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var q = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(q) ||
                                         p.Brand.ToLower().Contains(q) ||
                                         p.Series.ToLower().Contains(q) ||
                                         p.CPU.ToLower().Contains(q));
                ViewData["SearchQuery"] = search;
            }

            if (!string.IsNullOrEmpty(series) && series != "Tất cả")
            {
                query = query.Where(p => p.Series.ToLower().Contains(series.ToLower()));
                ViewData["ActiveSeries"] = series;
            }
            else
            {
                ViewData["ActiveSeries"] = "Tất cả";
            }

            if (!string.IsNullOrEmpty(brand) && brand != "Tất cả")
            {
                query = query.Where(p => p.Brand.ToLower() == brand.ToLower());
                ViewData["ActiveBrand"] = brand;
            }

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            // Sắp xếp theo Độ Hot (Mặc định) hoặc theo yêu cầu
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => (double)p.Price),
                "price_desc" => query.OrderByDescending(p => (double)p.Price),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.ViewCount)
                          .ThenByDescending(p => p.OriginalPrice > 0 ? (double)(p.OriginalPrice - p.Price) : 0)
                          .ThenByDescending(p => p.Id)
            };
            ViewData["ActiveSort"] = sort ?? "hot";

            // TỔNG SỐ SẢN PHẨM & PHÂN TRANG (6 sản phẩm / trang để chia thành 3-4 trang)
            int totalItems = await query.CountAsync();
            int pageSize = 6;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            page = Math.Max(1, Math.Min(page, totalPages));

            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;
            ViewData["PageSize"] = pageSize;

            ViewBag.SeriesList = await _context.Products
                .Select(p => p.Series).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.BrandList = await _context.Products
                .Select(p => p.Brand).Distinct().OrderBy(b => b).ToListAsync();

            if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirstValue("UserId"), out var userId))
            {
                var isFaceRecognized = User.FindFirstValue("LoginMethod") == "FaceId";
                ViewBag.PersonalizedRecommendations = await _recommendationService.GetForUserAsync(userId, isFaceRecognized);
            }

            // Cart count
            string sessionId = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? HttpContext.Session.Id
                : HttpContext.Session.Id;
            ViewBag.CartCount = await _context.CartItems
                .Where(c => c.SessionId == sessionId).SumAsync(c => (int?)c.Quantity) ?? 0;

            // ===== FLASH SALE (engine dùng chung với hệ thống tự động hóa) =====
            ViewBag.FlashSale = await _flashEngine.BuildAsync(_automationStore.GetSnapshot().FlashSoldOverrides);

            return View(products);
        }

        /// <summary>
        /// Action AJAX Trợ Lý AI Matchmaker — Tính điểm và gợi ý Top 3 Laptop ASUS phù hợp nhất.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetAiRecommendations(string usage, string budget, string feature)
        {
            var allProducts = await _context.Products.ToListAsync();

            var scored = allProducts.Select(p =>
            {
                int score = 72; // Base score

                // Usage criteria matching
                if (usage == "gaming" && (p.Series.Contains("ROG") || p.Series.Contains("TUF") || p.GPU.Contains("RTX"))) score += 18;
                else if (usage == "office" && (p.Series.Contains("ZenBook") || p.Series.Contains("VivoBook") || p.Weight.Contains("1.") || p.Weight.Contains("1kg"))) score += 18;
                else if (usage == "creative" && (p.Series.Contains("ProArt") || p.Series.Contains("ROG") || p.RAM.Contains("32") || p.RAM.Contains("64"))) score += 18;

                // Budget criteria matching
                if (budget == "under20" && p.Price <= 22000000) score += 6;
                else if (budget == "mid" && p.Price >= 18000000 && p.Price <= 38000000) score += 6;
                else if (budget == "flagship" && p.Price >= 32000000) score += 6;

                // Feature criteria matching
                if (feature == "display" && (p.ScreenResolution.Contains("OLED") || p.ScreenResolution.Contains("240Hz") || p.ScreenResolution.Contains("4K") || p.ScreenResolution.Contains("2.8K"))) score += 3;
                else if (feature == "gpu" && (p.GPU.Contains("4060") || p.GPU.Contains("4070") || p.GPU.Contains("4080") || p.GPU.Contains("4090"))) score += 3;
                else if (feature == "slim" && (p.Weight.Contains("1.") || p.Series.Contains("ZenBook"))) score += 3;

                int matchPercent = Math.Min(99, score);
                return new { Product = p, MatchPercent = matchPercent };
            })
            .OrderByDescending(x => x.MatchPercent)
            .ThenByDescending(x => x.Product.ViewCount)
            .Take(3)
            .Select(x => new
            {
                id = x.Product.Id,
                name = x.Product.Name,
                series = x.Product.Series,
                price = x.Product.Price.ToString("N0") + "₫",
                originalPrice = x.Product.OriginalPrice > 0 ? x.Product.OriginalPrice.ToString("N0") + "₫" : "",
                imageUrl = !string.IsNullOrEmpty(x.Product.ImageUrl) ? x.Product.ImageUrl : "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=600&q=80",
                cpu = x.Product.CPU,
                ram = x.Product.RAM,
                gpu = x.Product.GPU,
                matchPercent = x.MatchPercent
            })
            .ToList();

            return Json(new { success = true, items = scored });
        }

        /// <summary>
        /// Trang Trải Nghiệm Storytelling Interactive Experience (Scrollytelling Layout)
        /// </summary>
        [OutputCache(PolicyName = "FastReadPolicy")]
        public async Task<IActionResult> Story()
        {
            ViewData["Title"] = "ASUS Storytelling — Hành Trình Công Nghệ & Chế Tác";

            // Lấy các sản phẩm tiêu biểu thuộc các series khác nhau cho các chương trong Story
            var featuredProducts = await _context.Products
                .OrderByDescending(p => p.ViewCount)
                .Take(8)
                .ToListAsync();

            var rogProduct = featuredProducts.FirstOrDefault(p => p.Series.Contains("ROG")) ?? featuredProducts.FirstOrDefault();
            var zenbookProduct = featuredProducts.FirstOrDefault(p => p.Series.Contains("ZenBook")) ?? featuredProducts.ElementAtOrDefault(1);
            var proartProduct = featuredProducts.FirstOrDefault(p => p.Series.Contains("ProArt")) ?? featuredProducts.ElementAtOrDefault(2);

            ViewBag.RogProduct = rogProduct;
            ViewBag.ZenbookProduct = zenbookProduct;
            ViewBag.ProartProduct = proartProduct;

            // Flash sale data & Cart count
            string sessionId = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? HttpContext.Session.Id
                : HttpContext.Session.Id;
            ViewBag.CartCount = await _context.CartItems
                .Where(c => c.SessionId == sessionId).SumAsync(c => (int?)c.Quantity) ?? 0;

            ViewBag.FlashSale = await _flashEngine.BuildAsync(_automationStore.GetSnapshot().FlashSoldOverrides);

            return View(featuredProducts);
        }

        /// <summary>
        /// API Tìm Kiếm Nhanh Dành Cho Smart Command Palette (Ctrl + K)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Json(new { success = true, products = new List<object>() });
            }

            var query = q.Trim().ToLower();

            var products = await _context.Products
                .Where(p => p.Name.ToLower().Contains(query) ||
                            p.Brand.ToLower().Contains(query) ||
                            p.Series.ToLower().Contains(query) ||
                            p.CPU.ToLower().Contains(query) ||
                            p.GPU.ToLower().Contains(query))
                .OrderByDescending(p => p.ViewCount)
                .Take(6)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    series = p.Series,
                    price = p.Price.ToString("N0") + "₫",
                    originalPrice = p.OriginalPrice > 0 ? p.OriginalPrice.ToString("N0") + "₫" : "",
                    imageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=600&q=80",
                    cpu = p.CPU,
                    gpu = p.GPU,
                    ram = p.RAM,
                    estimatedFps = p.GPU.Contains("4090") ? 165 :
                                   p.GPU.Contains("4080") ? 140 :
                                   p.GPU.Contains("4070") ? 115 :
                                   p.GPU.Contains("4060") ? 95 :
                                   p.GPU.Contains("4050") ? 75 : 60
                })
                .ToListAsync();

            return Json(new { success = true, products });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
