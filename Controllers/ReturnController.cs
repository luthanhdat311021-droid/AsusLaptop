using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        private const int ReturnWindowDays = 30; // Trùng với Chính sách đổi trả (30 ngày)

        public ReturnController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private bool IsAdminOrSub() => User.Identity?.IsAuthenticated == true &&
                                        (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));

        private int? CurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return int.TryParse(claim, out int id) ? id : (int?)null;
        }

        // ══════════════════════ KHÁCH HÀNG ══════════════════════

        // GET /Return/Create/5  (5 = OrderId)
        [HttpGet]
        public async Task<IActionResult> Create(int id)
        {
            int orderId = id;
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Account", new { returnUrl = $"/Return/Create/{orderId}" });
            var userId = CurrentUserId();

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Variant)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) { TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!"; return RedirectToAction("MyOrders", "Account"); }
            if (order.UserId != userId) { TempData["ErrorMessage"] = "Bạn không có quyền với đơn hàng này!"; return RedirectToAction("MyOrders", "Account"); }

            if (order.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Chỉ có thể yêu cầu trả hàng/hoàn tiền với đơn hàng đã giao thành công.";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            int daysSince = (int)(DateTime.Now - order.OrderDate).TotalDays;
            if (daysSince > ReturnWindowDays)
            {
                TempData["ErrorMessage"] = $"Đơn hàng đã quá {ReturnWindowDays} ngày kể từ ngày mua, không thể yêu cầu trả hàng/hoàn tiền theo chính sách.";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            bool alreadyRequested = await _context.ReturnRequests.AnyAsync(r => r.OrderId == orderId && r.Status != "Rejected" && r.Status != "Cancelled");
            if (alreadyRequested)
            {
                TempData["ErrorMessage"] = "Đơn hàng này đã có yêu cầu trả hàng/hoàn tiền đang xử lý.";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }

            ViewBag.DaysLeft = ReturnWindowDays - daysSince;
            return View(order);
        }

        // POST /Return/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, string requestType, string reason, string? description,
            string? imageUrls, List<int>? selectedDetailIds, List<int>? quantities)
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) { TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!"; return RedirectToAction("MyOrders", "Account"); }
            if (order.Status != "Completed" || (DateTime.Now - order.OrderDate).TotalDays > ReturnWindowDays)
            {
                TempData["ErrorMessage"] = "Đơn hàng không đủ điều kiện trả hàng/hoàn tiền.";
                return RedirectToAction("OrderDetail", "Account", new { id = orderId });
            }
            if (selectedDetailIds == null || !selectedDetailIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm muốn trả.";
                return RedirectToAction(nameof(Create), new { orderId });
            }

            var request = new ReturnRequest
            {
                OrderId = orderId,
                UserId = userId.Value,
                RequestType = requestType,
                Reason = reason,
                Description = description,
                ImageUrls = imageUrls,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.ReturnRequests.Add(request);
            await _context.SaveChangesAsync();

            for (int i = 0; i < selectedDetailIds.Count; i++)
            {
                int detailId = selectedDetailIds[i];
                if (!order.OrderDetails.Any(d => d.Id == detailId)) continue; // chỉ chấp nhận item thuộc đúng đơn

                int qty = (quantities != null && i < quantities.Count && quantities[i] > 0) ? quantities[i] : 1;
                _context.ReturnRequestItems.Add(new ReturnRequestItem
                {
                    ReturnRequestId = request.Id,
                    OrderDetailId = detailId,
                    Quantity = qty
                });
            }
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAdminsAsync(
                "Yêu cầu trả hàng/hoàn tiền mới",
                $"Đơn hàng #{orderId} vừa có yêu cầu {request.RequestTypeVi.ToLower()} từ khách hàng.",
                "Order",
                $"/Admin/ReturnDetail/{request.Id}"
            );

            TempData["SuccessMessage"] = "Đã gửi yêu cầu! Chúng tôi sẽ xem xét và phản hồi sớm nhất.";
            return RedirectToAction(nameof(MyReturns));
        }

        // GET /Return/MyReturns
        [HttpGet]
        public async Task<IActionResult> MyReturns()
        {
            var userId = CurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var list = await _context.ReturnRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET /Return/Detail/5
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = CurrentUserId();
            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Items).ThenInclude(i => i.OrderDetail).ThenInclude(d => d!.Product)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            if (request.UserId != userId && !IsAdminOrSub()) return Forbid();

            return View(request);
        }

        // POST /Return/Cancel/5 — khách tự huỷ yêu cầu khi còn Pending
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = CurrentUserId();
            var request = await _context.ReturnRequests.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (request == null) return NotFound();
            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể huỷ yêu cầu khi đang chờ duyệt.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            request.Status = "Cancelled";
            request.ProcessedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã huỷ yêu cầu.";
            return RedirectToAction(nameof(MyReturns));
        }

        // ══════════════════════ ADMIN ══════════════════════

        [HttpGet]
        public async Task<IActionResult> AdminIndex(string? status)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var query = _context.ReturnRequests.Include(r => r.Order).Include(r => r.User).AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.Status = status;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> AdminDetail(int id)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .Include(r => r.Items).ThenInclude(i => i.OrderDetail).ThenInclude(d => d!.Product)
                .Include(r => r.Items).ThenInclude(i => i.OrderDetail).ThenInclude(d => d!.Variant)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, decimal refundAmount, string refundMethod, string? adminNote)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });

            var request = await _context.ReturnRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
            if (request.Status != "Pending") return Json(new { success = false, message = "Yêu cầu đã được xử lý trước đó" });

            request.Status = "Approved";
            request.RefundAmount = refundAmount;
            request.RefundMethod = refundMethod;
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.Now;
            request.ProcessedByUserId = CurrentUserId();
            await _context.SaveChangesAsync();

            await _notificationService.NotifyUserAsync(
                request.UserId,
                "Yêu cầu trả hàng đã được duyệt",
                $"Yêu cầu {request.RequestTypeVi.ToLower()} cho đơn #{request.OrderId} đã được duyệt. Vui lòng gửi sản phẩm về theo hướng dẫn.",
                "Order",
                $"/Return/Detail/{request.Id}"
            );

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string adminNote)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });

            var request = await _context.ReturnRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
            if (request.Status != "Pending") return Json(new { success = false, message = "Yêu cầu đã được xử lý trước đó" });

            request.Status = "Rejected";
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.Now;
            request.ProcessedByUserId = CurrentUserId();
            await _context.SaveChangesAsync();

            await _notificationService.NotifyUserAsync(
                request.UserId,
                "Yêu cầu trả hàng bị từ chối",
                $"Yêu cầu {request.RequestTypeVi.ToLower()} cho đơn #{request.OrderId} đã bị từ chối. Lý do: {adminNote}",
                "Order",
                $"/Return/Detail/{request.Id}"
            );

            return Json(new { success = true });
        }

        // Admin xác nhận đã nhận hàng hoàn + hoàn tiền xong -> nhập lại kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRefunded(int id)
        {
            if (!IsAdminOrSub()) return Json(new { success = false, message = "Không có quyền" });

            var request = await _context.ReturnRequests
                .Include(r => r.Items).ThenInclude(i => i.OrderDetail)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
            if (request.Status != "Approved") return Json(new { success = false, message = "Chỉ áp dụng cho yêu cầu đã duyệt" });

            // Nhập lại kho cho từng sản phẩm được trả (nếu là Return/Exchange, không áp dụng cho Refund thuần)
            if (request.RequestType != "Refund")
            {
                foreach (var item in request.Items)
                {
                    var detail = item.OrderDetail;
                    if (detail == null) continue;

                    int stockAfter;
                    if (detail.VariantId != null)
                    {
                        var variant = await _context.ProductVariants.FindAsync(detail.VariantId);
                        if (variant == null) continue;
                        variant.Stock += item.Quantity;
                        stockAfter = variant.Stock;
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(detail.ProductId);
                        if (product == null) continue;
                        product.Quantity += item.Quantity;
                        stockAfter = product.Quantity;
                    }

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = detail.ProductId,
                        VariantId = detail.VariantId,
                        QuantityChange = item.Quantity,
                        StockAfter = stockAfter,
                        Reason = "Return",
                        Note = $"Nhập lại kho từ yêu cầu trả hàng #{request.Id} (đơn #{request.OrderId})",
                        OrderId = request.OrderId,
                        CreatedByUserId = CurrentUserId(),
                        CreatedAt = DateTime.Now
                    });
                }
            }

            request.Status = "Refunded";
            request.ProcessedAt = DateTime.Now;

            // Cập nhật trạng thái đơn hàng gốc + gián tiếp trừ doanh thu:
            // Dashboard tính "Tổng doanh thu" bằng cách cộng TotalAmount của các đơn có
            // Status == "Completed", nên chỉ cần chuyển đơn sang "Refunded" là doanh thu
            // tự động không còn tính đơn này nữa (không cần cộng/trừ tay ở nơi khác).
            if (request.Order != null && request.Order.Status != "Refunded")
            {
                request.Order.Status = "Refunded";
            }

            await _context.SaveChangesAsync();

            await _notificationService.NotifyUserAsync(
                request.UserId,
                "Đã hoàn tất hoàn tiền/trả hàng",
                $"Yêu cầu {request.RequestTypeVi.ToLower()} cho đơn #{request.OrderId} đã hoàn tất. Số tiền hoàn: {request.RefundAmount?.ToString("N0")}₫.",
                "Order",
                $"/Return/Detail/{request.Id}"
            );

            return Json(new { success = true });
        }
    }
}
