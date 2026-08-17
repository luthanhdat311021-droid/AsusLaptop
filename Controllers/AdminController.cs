using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    [Authorize(Roles = "Admin,SubAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(ApplicationDbContext context, EmailService emailService, NotificationService notificationService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
            _httpClientFactory = httpClientFactory;
        }

        private bool IsAdmin() => User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

        /// <summary>
        /// Xuất ảnh sản phẩm hiện có trong DB, phân loại theo thư mục theo dòng (Series),
        /// đóng gói thành 1 file .zip để dùng làm dữ liệu train Roboflow.
        /// Lưu ý: mỗi sản phẩm hiện chỉ có 1 ảnh trong DB, nên số ảnh xuất ra = số sản phẩm.
        /// Muốn có bộ dữ liệu train tốt (khuyến nghị 50-100+ ảnh/lớp), nên dùng thêm tính năng
        /// Augmentation có sẵn của Roboflow khi tạo Version cho dataset (xoay, lật, đổi sáng tối...).
        /// </summary>
        public async Task<IActionResult> ExportProductImagesForTraining()
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var products = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.ImageUrl))
                .Select(p => new { p.Id, p.Name, p.Series, p.ImageUrl })
                .ToListAsync();

            var tempRoot = Path.Combine(Path.GetTempPath(), "roboflow_export_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            int okCount = 0, failCount = 0;

            foreach (var p in products)
            {
                try
                {
                    // Tên thư mục = tên dòng sản phẩm, dọn ký tự không hợp lệ cho tên thư mục
                    var seriesFolder = string.IsNullOrWhiteSpace(p.Series) ? "Khac" : p.Series;
                    foreach (var c in Path.GetInvalidFileNameChars()) seriesFolder = seriesFolder.Replace(c, '_');
                    seriesFolder = seriesFolder.Replace(' ', '_');

                    var folderPath = Path.Combine(tempRoot, seriesFolder);
                    Directory.CreateDirectory(folderPath);

                    // ImageUrl có thể là URL tuyệt đối (http...) hoặc đường dẫn tương đối (/image/...)
                    string absoluteUrl = p.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? p.ImageUrl
                        : $"{Request.Scheme}://{Request.Host}{(p.ImageUrl.StartsWith("/") ? "" : "/")}{p.ImageUrl}";

                    var bytes = await client.GetByteArrayAsync(absoluteUrl);

                    var ext = Path.GetExtension(new Uri(absoluteUrl).AbsolutePath);
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";

                    var safeName = string.Concat($"{p.Id}_{p.Name}".Split(Path.GetInvalidFileNameChars()));
                    var filePath = Path.Combine(folderPath, safeName + ext);

                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    okCount++;
                }
                catch
                {
                    failCount++; // bỏ qua sản phẩm lỗi ảnh, tiếp tục các sản phẩm khác
                }
            }

            var zipPath = tempRoot + ".zip";
            System.IO.Compression.ZipFile.CreateFromDirectory(tempRoot, zipPath);

            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);

            // Dọn file tạm
            try { Directory.Delete(tempRoot, true); } catch { }
            try { System.IO.File.Delete(zipPath); } catch { }

            TempData["ExportImageStats"] = $"Xuất thành công {okCount} ảnh, {failCount} ảnh lỗi/không tải được.";

            return File(zipBytes, "application/zip", $"product-images-for-roboflow-{DateTime.Now:yyyyMMdd-HHmm}.zip");
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var completedOrders = await _context.Orders.Where(o => o.Status == "Completed").ToListAsync();
            ViewBag.TotalRevenue = completedOrders.Sum(o => o.TotalAmount);
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.StockAlertCount = await _context.Products.Where(p => p.Quantity < 5).CountAsync();
            ViewBag.PendingOrders = await _context.Orders.Where(o => o.Status == "Pending").CountAsync();

            var startDate = DateTime.Today.AddDays(-7);
            var raw7days = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.Status == "Completed")
                .Select(o => new { o.OrderDate, o.TotalAmount }).ToListAsync();

            var grouped = raw7days.GroupBy(o => o.OrderDate.ToString("dd/MM")).OrderBy(g => g.Key).ToList();
            ViewBag.ChartLabels = grouped.Select(g => g.Key).ToList();
            ViewBag.ChartData = grouped.Select(g => g.Sum(o => o.TotalAmount)).ToList();

            var rawTop = await _context.OrderDetails
                .Include(d => d.Product)
                .Where(d => d.Order!.Status == "Completed")
                .ToListAsync();

            ViewBag.TopProducts = rawTop
                .GroupBy(d => d.Product!.Name)
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.Quantity * d.Price)
                })
                .OrderByDescending(p => p.QuantitySold).Take(5).ToList();

            var allOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(allOrders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                if (order.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"Đơn hàng #{orderId} đã bị hủy, không thể thay đổi trạng thái nữa.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var previousStatus = order.Status;
                order.Status = status;
                // Auto-mark payment as paid when completed
                if (status == "Completed" && order.PaymentMethod == "BankTransfer")
                    order.PaymentStatus = "Paid";
                await _context.SaveChangesAsync();

                // Gửi email thông báo "đang giao hàng" — chỉ gửi khi vừa chuyển sang Shipped
                // (tránh gửi trùng nếu admin lưu lại cùng trạng thái)
                if (status == "Shipped" && previousStatus != "Shipped" && !string.IsNullOrWhiteSpace(order.Email))
                {
                    var details = await _context.OrderDetails
                        .Include(d => d.Product)
                        .Include(d => d.Variant)
                        .Where(d => d.OrderId == orderId)
                        .ToListAsync();

                    _ = Task.Run(async () =>
                    {
                        try { await _emailService.SendShippingEmailAsync(order, details); }
                        catch { /* không chặn luồng admin nếu gửi mail lỗi */ }
                    });
                }

                TempData["SuccessMessage"] = $"Cập nhật đơn hàng #{orderId} thành {status}!";

                // Thông báo realtime cho khách hàng nếu trạng thái thực sự thay đổi
                if (order.UserId.HasValue && previousStatus != status)
                {
                    string label = status switch
                    {
                        "Processing" => "đang được chuẩn bị",
                        "Shipped"    => "đang được giao",
                        "Completed"  => "đã giao thành công",
                        "Cancelled"  => "đã bị huỷ",
                        _            => "đã cập nhật trạng thái"
                    };
                    await _notificationService.NotifyUserAsync(
                        order.UserId.Value,
                        "Cập nhật đơn hàng",
                        $"Đơn hàng #{order.Id} của bạn {label}.",
                        "Order",
                        $"/Account/OrderDetail/{order.Id}"
                    );
                }
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int orderId, string paymentStatus)
        {
            if (!IsAdmin()) return Json(new { success = false });
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = paymentStatus;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> InventoryLogs(int? productId, string? reason, int page = 1)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var query = _context.InventoryLogs
                .Include(l => l.Product)
                .Include(l => l.Variant)
                .Include(l => l.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);
            if (!string.IsNullOrEmpty(reason)) query = query.Where(l => l.Reason == reason);

            int pageSize = 30;
            int totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Products = new SelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", productId);
            ViewBag.Reason = reason;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return View(logs);
        }

        public async Task<IActionResult> Products(string? search)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search) || p.Series.Contains(search));
            ViewBag.Search = search;
            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int productId, int quantity)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });
            if (quantity < 0) return Json(new { success = false, message = "Số lượng không hợp lệ" });
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            int delta = quantity - product.Quantity;
            product.Quantity = quantity;

            if (delta != 0)
            {
                _context.InventoryLogs.Add(new InventoryLog
                {
                    ProductId       = productId,
                    QuantityChange  = delta,
                    StockAfter      = quantity,
                    Reason          = "Adjustment",
                    Note            = "Admin chỉnh sửa tồn kho trực tiếp",
                    CreatedByUserId = int.TryParse(User.FindFirst("UserId")?.Value, out int uid) ? uid : (int?)null,
                    CreatedAt       = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            string stockClass = quantity < 5 ? "danger" : quantity < 15 ? "warning" : "success";
            return Json(new { success = true, quantity, stockClass });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var product = await _context.Products.FindAsync(id);
            if (product == null) { TempData["ErrorMessage"] = "Sản phẩm không tồn tại!"; return RedirectToAction(nameof(Products)); }

            bool hasOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == id);
            if (hasOrders) { TempData["ErrorMessage"] = $"Không thể xóa '{product.Name}' vì đã có trong đơn hàng!"; return RedirectToAction(nameof(Products)); }

            _context.CartItems.RemoveRange(_context.CartItems.Where(c => c.ProductId == id));
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa '{product.Name}'!";
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetSubAdmin(int userId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại" });
            if (user.Role == "Admin") return Json(new { success = false, message = "Không thể thay đổi quyền Admin" });

            user.Role = "SubAdmin";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã cấp quyền Phó Admin cho \"{user.Username}\"" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSubAdmin(int userId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại" });
            if (user.Role == "Admin") return Json(new { success = false, message = "Không thể thay đổi quyền Admin" });

            user.Role = "Customer";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã thu hồi quyền Phó Admin của \"{user.Username}\"" });
        }
        
        private bool IsAdminOrSub() => User.Identity?.IsAuthenticated == true && (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));
    }
}
