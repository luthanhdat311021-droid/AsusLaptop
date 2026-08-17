using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using System.Text.Json;

namespace AsusLaptop.Controllers
{
    public class CompareController : Controller
    {
        private const string SessionKey = "CompareIds";
        private const int MaxItems = 4;

        private readonly ApplicationDbContext _context;

        public CompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Helpers ──────────────────────────────────────────────
        private List<int> GetIds()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new List<int>();
            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private void SaveIds(List<int> ids)
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ids));
        }

        // ── Trang so sánh ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var ids = GetIds();
            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            // Giữ đúng thứ tự người dùng đã thêm
            products = ids.Select(id => products.FirstOrDefault(p => p.Id == id))
                           .Where(p => p != null)
                           .Select(p => p!)
                           .ToList();

            ViewBag.MaxItems = MaxItems;
            return View(products);
        }

        // ── Thêm sản phẩm vào danh sách so sánh (AJAX) ──────────
        [HttpPost]
        public async Task<IActionResult> Add(int id)
        {
            var exists = await _context.Products.AnyAsync(p => p.Id == id);
            if (!exists) return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            var ids = GetIds();
            if (ids.Contains(id))
                return Json(new { success = true, count = ids.Count, message = "Sản phẩm đã có trong danh sách so sánh." });

            if (ids.Count >= MaxItems)
                return Json(new { success = false, count = ids.Count, message = $"Chỉ có thể so sánh tối đa {MaxItems} sản phẩm. Vui lòng bỏ bớt trước khi thêm mới." });

            ids.Add(id);
            SaveIds(ids);

            return Json(new { success = true, count = ids.Count, message = "Đã thêm vào danh sách so sánh." });
        }

        // ── Xoá 1 sản phẩm khỏi danh sách so sánh (AJAX) ────────
        [HttpPost]
        public IActionResult Remove(int id)
        {
            var ids = GetIds();
            ids.Remove(id);
            SaveIds(ids);
            return Json(new { success = true, count = ids.Count });
        }

        // ── Xoá toàn bộ danh sách so sánh (AJAX) ─────────────────
        [HttpPost]
        public IActionResult Clear()
        {
            SaveIds(new List<int>());
            return Json(new { success = true, count = 0 });
        }

        // ── Lấy danh sách rút gọn để hiển thị thanh so sánh nổi ──
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var ids = GetIds();
            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ImageUrl,
                    p.Series,
                    p.Price
                })
                .ToListAsync();

            products = ids.Select(id => products.FirstOrDefault(p => p.Id == id))
                           .Where(p => p != null)
                           .Select(p => p!)
                           .ToList();

            return Json(new { count = ids.Count, max = MaxItems, items = products });
        }
    }
}
