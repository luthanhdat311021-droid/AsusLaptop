using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Controllers
{
    [Authorize(Roles = "Admin,SubAdmin")]
    public class AdminVariantController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AdminVariantController(ApplicationDbContext db) => _db = db;

        // GET /AdminVariant?productId=1
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .Include(v => v.SerialNumbers)
                .OrderBy(v => v.RAM).ThenBy(v => v.Color)
                .ToListAsync();

            ViewBag.Product = product;
            return View(variants);
        }

        // GET /AdminVariant/Create?productId=1
        [HttpGet]
        public async Task<IActionResult> Create(int productId)
        {
            ViewBag.Product = await _db.Products.FindAsync(productId);
            return View(new ProductVariant { ProductId = productId, IsDefault = false });
        }

        // POST /AdminVariant/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVariant v, int initialStock = 5)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Product = await _db.Products.FindAsync(v.ProductId);
                return View(v);
            }

            if (v.IsDefault) await ClearDefault(v.ProductId);

            v.Stock     = initialStock;
            v.CreatedAt = DateTime.Now;
            _db.ProductVariants.Add(v);
            await _db.SaveChangesAsync();

            // Tự động sinh serial
            var product = await _db.Products.FindAsync(v.ProductId);
            var startSeq = await NextSerialStartSeq(product!.Series);
            var serials = SerialNumberGenerator.GenerateBatch(product!.Series, initialStock, startSeq)
                .Select(s => new SerialNumber
                {
                    SerialNo  = s,
                    ProductId = v.ProductId,
                    VariantId = v.Id,
                    Status    = "Available",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();
            _db.SerialNumbers.AddRange(serials);
            await SyncProductQuantity(v.ProductId);

            if (initialStock > 0)
            {
                _db.InventoryLogs.Add(new InventoryLog
                {
                    ProductId      = v.ProductId,
                    VariantId      = v.Id,
                    QuantityChange = initialStock,
                    StockAfter     = v.Stock,
                    Reason         = "Import",
                    Note           = $"Nhập kho ban đầu cho biến thể mới '{v.DisplayLabel}'",
                    CreatedByUserId = CurrentUserId(),
                    CreatedAt      = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm biến thể '{v.DisplayLabel}' và {initialStock} serial.";
            return RedirectToAction("Index", new { productId = v.ProductId });
        }

        // GET /AdminVariant/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var v = await _db.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.SerialNumbers)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            return View(v);
        }

        // POST /AdminVariant/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductVariant form, int addStock = 0)
        {
            var v = await _db.ProductVariants
                .Include(x => x.SerialNumbers)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();

            if (form.IsDefault) await ClearDefault(v.ProductId, id);

            v.RAM         = form.RAM;
            v.Color       = form.Color;
            v.ColorHex    = form.ColorHex;
            v.PriceAdjust = form.PriceAdjust;
            v.IsDefault   = form.IsDefault;

            if (addStock > 0)
            {
                var product = await _db.Products.FindAsync(v.ProductId);
                var startSeq = await NextSerialStartSeq(product!.Series);
                var serials = SerialNumberGenerator.GenerateBatch(product!.Series, addStock, startSeq)
                    .Select(s => new SerialNumber
                    {
                        SerialNo  = s,
                        ProductId = v.ProductId,
                        VariantId = v.Id,
                        Status    = "Available",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }).ToList();
                _db.SerialNumbers.AddRange(serials);
                v.Stock += addStock;

                _db.InventoryLogs.Add(new InventoryLog
                {
                    ProductId      = v.ProductId,
                    VariantId      = v.Id,
                    QuantityChange = addStock,
                    StockAfter     = v.Stock,
                    Reason         = "Import",
                    Note           = $"Nhập thêm hàng cho biến thể '{v.DisplayLabel}'",
                    CreatedByUserId = CurrentUserId(),
                    CreatedAt      = DateTime.Now
                });
            }

            await SyncProductQuantity(v.ProductId);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật biến thể.";
            return RedirectToAction("Index", new { productId = v.ProductId });
        }

        // POST /AdminVariant/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.ProductVariants
                .Include(x => x.SerialNumbers)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();

            if (v.SerialNumbers.Any(s => s.Status == "Sold"))
            {
                TempData["ErrorMessage"] = "Không thể xóa biến thể đã có serial đã bán!";
                return RedirectToAction("Index", new { productId = v.ProductId });
            }
            int pid = v.ProductId;
            _db.SerialNumbers.RemoveRange(v.SerialNumbers);
            _db.ProductVariants.Remove(v);
            await _db.SaveChangesAsync();
            await SyncProductQuantity(pid);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa biến thể.";
            return RedirectToAction("Index", new { productId = pid });
        }

        // GET /AdminVariant/Serials/5
        [HttpGet]
        public async Task<IActionResult> Serials(int variantId)
        {
            var v = await _db.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.SerialNumbers)
                .FirstOrDefaultAsync(x => x.Id == variantId);
            if (v == null) return NotFound();
            return View(v);
        }

        private int? CurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return int.TryParse(claim, out int id) ? id : (int?)null;
        }

        private async Task ClearDefault(int productId, int? excludeId = null)
        {
            var list = await _db.ProductVariants
                .Where(x => x.ProductId == productId && x.IsDefault && (excludeId == null || x.Id != excludeId))
                .ToListAsync();
            list.ForEach(x => x.IsDefault = false);
        }

        // Đồng bộ Product.Quantity = tổng tồn kho của tất cả biến thể
        // (để số hiển thị ở danh sách Quản lý sản phẩm luôn khớp với tổng biến thể)
        private async Task SyncProductQuantity(int productId)
        {
            var totalStock = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .SumAsync(v => v.Stock);

            var product = await _db.Products.FindAsync(productId);
            if (product != null) product.Quantity = totalStock;
        }

        // Tính số thứ tự serial tiếp theo dựa trên DB (không dùng biến đếm RAM)
        // để tránh sinh trùng serial khi ứng dụng khởi động lại.
        private async Task<int> NextSerialStartSeq(string seriesPrefix)
        {
            var pattern = SerialNumberGenerator.BuildPrefixPattern(seriesPrefix); // "ASU-EXP-26-"
            var existing = await _db.SerialNumbers
                .Where(s => s.SerialNo.StartsWith(pattern))
                .Select(s => s.SerialNo)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var s in existing)
            {
                var tail = s.Length > pattern.Length ? s[pattern.Length..] : "";
                if (int.TryParse(tail, out int n) && n > maxSeq) maxSeq = n;
            }
            return maxSeq + 1;
        }
    }
}
