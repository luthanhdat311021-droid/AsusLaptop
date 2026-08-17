using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    public class ImageSearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoboflowService _roboflowService;

        public ImageSearchController(ApplicationDbContext context, RoboflowService roboflowService)
        {
            _context = context;
            _roboflowService = roboflowService;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Search(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                ViewBag.Error = "Vui lòng chọn ảnh để tìm kiếm.";
                return View("Index");
            }

            // Kiểm tra định dạng file
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
            {
                ViewBag.Error = "Chỉ hỗ trợ định dạng JPG, PNG, WEBP.";
                return View("Index");
            }

            // Gọi Roboflow
            var result = await _roboflowService.DetectFromImageAsync(image);

            if (result == null || result.Predictions.Count == 0)
            {
                ViewBag.Error = "Không nhận diện được laptop trong ảnh. Vui lòng thử ảnh khác.";
                ViewBag.Products = new List<Product>();
                return View("Index");
            }

            // Lấy prediction có confidence cao nhất
            var topPrediction = result.Predictions
                .OrderByDescending(p => p.Confidence)
                .First();

            ViewBag.DetectedClass = topPrediction.Class;
            ViewBag.Confidence = (topPrediction.Confidence * 100).ToString("F1");

            // Tìm sản phẩm trong DB khớp với class nhận diện
            // Class thường là tên dòng như "ROG", "ZenBook", "VivoBook", v.v.
            var keyword = topPrediction.Class.ToLower().Replace("_", " ").Replace("-", " ");

            var products = await _context.Products
                .Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    p.Series.ToLower().Contains(keyword) ||
                    p.Brand.ToLower().Contains(keyword) ||
                    p.Description.ToLower().Contains(keyword))
                .Take(12)
                .ToListAsync();

            // Nếu không tìm thấy khớp chính xác, thử từng từ trong keyword
            if (products.Count == 0)
            {
                var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length < 3) continue;
                    var w = word;
                    products = await _context.Products
                        .Where(p =>
                            p.Name.ToLower().Contains(w) ||
                            p.Series.ToLower().Contains(w) ||
                            p.Brand.ToLower().Contains(w))
                        .Take(12)
                        .ToListAsync();

                    if (products.Count > 0) break;
                }
            }

            // Cart count
            string sessionId = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? HttpContext.Session.Id
                : HttpContext.Session.Id;
            ViewBag.CartCount = await _context.CartItems
                .Where(c => c.SessionId == sessionId).SumAsync(c => (int?)c.Quantity) ?? 0;

            ViewBag.Products = products;
            ViewBag.AllPredictions = result.Predictions
                .OrderByDescending(p => p.Confidence)
                .Take(5)
                .ToList();

            return View("Index");
        }
    }
}