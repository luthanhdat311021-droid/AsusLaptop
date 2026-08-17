using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;
using QRCoder;

namespace AsusLaptop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductDescriptionAiService _descriptionAi;
        private readonly FutureFitAiService _futureFitAi;
        private readonly ProductAutoFillAiService _autoFillAi;

        public ProductController(ApplicationDbContext context, ProductDescriptionAiService descriptionAi, FutureFitAiService futureFitAi, ProductAutoFillAiService autoFillAi)
        {
            _context = context;
            _descriptionAi = descriptionAi;
            _futureFitAi = futureFitAi;
            _autoFillAi = autoFillAi;
        }

        // ── Helper: nạp dropdown Category & Brand vào ViewBag ────────────
        private async Task LoadCategoryBrandViewBag(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name", selectedCategoryId);

            ViewBag.Brands = new SelectList(
                await _context.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync(),
                "Id", "Name", selectedBrandId);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BrandRef)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            // Tăng lượt xem
            product.ViewCount++;
            await _context.SaveChangesAsync();

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == id)
                .Include(v => v.SerialNumbers)
                .OrderBy(v => v.IsDefault ? 0 : 1)
                .ThenBy(v => v.RAM)
                .ThenBy(v => v.Color)
                .ToListAsync();
            ViewBag.Variants = variants;

            var gallery = await _context.ProductImages
                .Where(i => i.ProductId == id)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .ToListAsync();
            ViewBag.Gallery = gallery;

            var related = await _context.Products
                .Where(p => p.Id != id && (p.Series == product.Series || p.Brand == product.Brand))
                .Take(4).ToListAsync();
            ViewBag.RelatedProducts = related;

            string sessionId = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? HttpContext.Session.Id
                : HttpContext.Session.Id;
            ViewBag.CartCount = await _context.CartItems
                .Where(c => c.SessionId == sessionId).SumAsync(c => (int?)c.Quantity) ?? 0;

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
            ViewBag.ReviewCount = reviews.Count;

            ViewBag.IsInWishlist = false;
            ViewBag.HasReviewed = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (currentUser != null)
                {
                    ViewBag.IsInWishlist = await _context.WishlistItems
                        .AnyAsync(w => w.UserId == currentUser.Id && w.ProductId == id);
                    ViewBag.HasReviewed = reviews.Any(r => r.UserId == currentUser.Id);
                }
            }

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Specs(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đánh giá sản phẩm.";
                return RedirectToAction("Details", new { id = productId });
            }
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction("Details", new { id = productId });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.CreatedAt = DateTime.Now;
            }
            else
            {
                _context.Reviews.Add(new Review
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId, int productId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Details", new { id = productId });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review != null && user != null && (review.UserId == user.Id || User.IsInRole("Admin")))
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = productId });
        }

        [HttpGet]
        public IActionResult QrCode(int id, int size = 10)
        {
            string targetUrl = Url.Action("Specs", "Product", new { id }, Request.Scheme)
                                ?? $"{Request.Scheme}://{Request.Host}/Product/Specs/{id}";

            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(targetUrl, QRCodeGenerator.ECCLevel.Q);
            var pngQr = new PngByteQRCode(qrData);
            byte[] qrBytes = pngQr.GetGraphic(size);

            return File(qrBytes, "image/png");
        }

        // ── CREATE ───────────────────────────────────────────────────────
        [Authorize(Roles = "Admin,SubAdmin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoryBrandViewBag();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(product.ImageUrl))
                    product.ImageUrl = "https://cdn.asus.com/media/global/products/ux3405ma/ux3405ma_01.png";

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Products", "Admin");
            }
            await LoadCategoryBrandViewBag(product.CategoryId, product.BrandId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        public IActionResult GenerateDescription([FromBody] ProductDescriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Hãy nhập tên sản phẩm trước khi tạo mô tả." });

            return Json(new { description = _descriptionAi.Generate(request) });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        public async Task<IActionResult> AutoFillFromName([FromBody] ProductAutoFillRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 5)
                return BadRequest(new { message = "Hãy nhập tên máy cụ thể hơn." });
            var details = await _autoFillAi.GetDetailsAsync(request.Name);
            return details == null ? StatusCode(502, new { message = "Chưa thể tìm cấu hình tự động. Hãy thử lại hoặc nhập thủ công." }) : Json(details);
        }

        [HttpPost]
        public async Task<IActionResult> FutureFit([FromBody] FutureFitRequest request)
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return NotFound();
            return Json(_futureFitAi.Analyze(product, request.Scenario, request.Years));
        }

        // ── EDIT ─────────────────────────────────────────────────────────
        [Authorize(Roles = "Admin,SubAdmin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            await LoadCategoryBrandViewBag(product.CategoryId, product.BrandId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Products", "Admin");
            }
            await LoadCategoryBrandViewBag(product.CategoryId, product.BrandId);
            return View(product);
        }

        // ══════════════ THƯ VIỆN ẢNH SẢN PHẨM (ProductImage) ══════════════
        [HttpGet]
        [Authorize(Roles = "Admin,SubAdmin")]
        public async Task<IActionResult> Images(int productId)
        {
            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.ImageUrl, i.AltText, i.IsPrimary, i.SortOrder })
                .ToListAsync();
            return Json(images);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImage(int productId, string imageUrl, string? altText)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return Json(new { success = false, message = "Vui lòng nhập URL ảnh" });

            bool hasAny = await _context.ProductImages.AnyAsync(i => i.ProductId == productId);
            int maxSort = await _context.ProductImages.Where(i => i.ProductId == productId)
                .Select(i => (int?)i.SortOrder).MaxAsync() ?? 0;

            var img = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl.Trim(),
                AltText = altText,
                IsPrimary = !hasAny,
                SortOrder = maxSort + 1,
                CreatedAt = DateTime.Now
            };
            _context.ProductImages.Add(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = img.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _context.ProductImages.FindAsync(id);
            if (img == null) return Json(new { success = false });

            bool wasPrimary = img.IsPrimary;
            int productId = img.ProductId;
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();

            if (wasPrimary)
            {
                var next = await _context.ProductImages.Where(i => i.ProductId == productId)
                    .OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
                if (next != null) { next.IsPrimary = true; await _context.SaveChangesAsync(); }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SubAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int id)
        {
            var img = await _context.ProductImages.FindAsync(id);
            if (img == null) return Json(new { success = false });

            var siblings = await _context.ProductImages.Where(i => i.ProductId == img.ProductId).ToListAsync();
            foreach (var s in siblings) s.IsPrimary = (s.Id == id);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
