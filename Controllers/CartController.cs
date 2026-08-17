using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VnPayService _vnPay;
        private readonly MomoService _momo;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public CartController(ApplicationDbContext context, VnPayService vnPay, MomoService momo, EmailService emailService, NotificationService notificationService)
        {
            _context      = context;
            _vnPay        = vnPay;
            _momo         = momo;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        private string GetCartSessionId()
        {
            if (User.Identity?.IsAuthenticated == true)
                return User.Identity.Name ?? HttpContext.Session.Id;
            HttpContext.Session.SetString("SessionKey", "Init");
            return HttpContext.Session.Id;
        }

        /// <summary>
        /// Cho phép khách "Tiếp tục thanh toán" lại 1 đơn hàng VNPay/MoMo đang chờ thanh toán,
        /// thay vì phải đặt hàng lại từ đầu. Chỉ áp dụng khi đơn còn "Pending" và chưa thanh toán —
        /// đơn đã bị OrderAutoCancelService tự hủy (quá 15 phút) sẽ không cho thanh toán tiếp nữa.
        /// </summary>
        public async Task<IActionResult> ContinuePayment(int orderId)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Account");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("MyOrders", "Account");
            }

            if (order.Status != "Pending" || order.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] = $"Đơn hàng #{orderId} không còn ở trạng thái chờ thanh toán (có thể đã bị hủy do quá 15 phút hoặc đã thanh toán xong).";
                return RedirectToAction("MyOrders", "Account");
            }

            if (order.PaymentMethod == "VNPay")
            {
                var payUrl = _vnPay.CreatePaymentUrl(
                    order.Id, order.TotalAmount,
                    $"Thanh toan don hang #{order.Id} - ASUS Laptop Store");
                return Redirect(payUrl);
            }

            if (order.PaymentMethod == "Momo")
            {
                var (success, momoPayUrl, message) = await _momo.CreatePaymentAsync(
                    order.Id, order.TotalAmount,
                    $"Thanh toan don hang #{order.Id} - ASUS Laptop Store");

                if (success) return Redirect(momoPayUrl);

                TempData["ErrorMessage"] = $"Không thể khởi tạo lại thanh toán MoMo: {message}. Vui lòng thử lại sau ít phút.";
                return RedirectToAction("MyOrders", "Account");
            }

            TempData["ErrorMessage"] = "Đơn hàng này không sử dụng phương thức thanh toán trực tuyến.";
            return RedirectToAction("MyOrders", "Account");
        }

        public async Task<IActionResult> Index()
        {
            string sessionId = GetCartSessionId();
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            // Giá mỗi item = giá sản phẩm + chênh lệch biến thể
            ViewBag.CartTotal = cartItems.Sum(i =>
                ((i.Product?.Price ?? 0) + (i.Variant?.PriceAdjust ?? 0)) * i.Quantity);
            ViewBag.CartCount = cartItems.Sum(i => i.Quantity);

            // Voucher đang áp dụng (nếu có, lưu trong session)
            ViewBag.VoucherCode = HttpContext.Session.GetString("VoucherCode");
            ViewBag.VoucherDiscount = decimal.TryParse(HttpContext.Session.GetString("VoucherDiscount"), out var d) ? d : 0;

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ProductVariant? variant = variantId.HasValue ? await _context.ProductVariants.FindAsync(variantId.Value) : null;
            int availableStock = variant != null ? variant.Stock : product.Quantity;
            if (availableStock <= 0)
            {
                TempData["ErrorMessage"] = $"{product.Name} hiện đã hết hàng.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            string sessionId = GetCartSessionId();

            // Giỏ hàng phân biệt theo productId + variantId
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sessionId
                                       && c.ProductId == productId
                                       && c.VariantId == variantId);

            int requestedTotal = (cartItem?.Quantity ?? 0) + quantity;
            if (requestedTotal > availableStock)
            {
                TempData["ErrorMessage"] = $"{product.Name} chỉ còn {availableStock} sản phẩm trong kho.";
                quantity = Math.Max(0, availableStock - (cartItem?.Quantity ?? 0));
                if (quantity == 0) return RedirectToAction("Index");
            }

            if (cartItem == null)
                _context.CartItems.Add(new CartItem
                {
                    SessionId = sessionId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity  = quantity
                });
            else
                cartItem.Quantity += quantity;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int productId, int? variantId = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            string sessionId = GetCartSessionId();
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sessionId
                                       && c.ProductId == productId
                                       && c.VariantId == variantId);

            if (cartItem == null)
                _context.CartItems.Add(new CartItem
                {
                    SessionId = sessionId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity  = 1
                });
            else
                cartItem.Quantity += 1;

            await _context.SaveChangesAsync();

            int totalItems = await _context.CartItems
                .Where(c => c.SessionId == sessionId).SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount = totalItems, productName = product.Name });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantityAjax(int productId, int quantity, int? variantId = null)
        {
            if (quantity < 1) return Json(new { success = false, message = "Số lượng phải ít nhất là 1." });
            string sessionId = GetCartSessionId();
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sessionId
                                       && c.ProductId == productId
                                       && c.VariantId == variantId);
            if (cartItem != null) { cartItem.Quantity = quantity; await _context.SaveChangesAsync(); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId, int? variantId = null)
        {
            string sessionId = GetCartSessionId();
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sessionId
                                       && c.ProductId == productId
                                       && c.VariantId == variantId);
            if (cartItem != null) { _context.CartItems.Remove(cartItem); await _context.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }

        // ─── VOUCHER ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string code, decimal cartTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code.ToUpper() == code.Trim().ToUpper());

            // Tự động nhận diện mã Flash Sale ASUSFLASH30 và Voucher VIP ASUSVIP1M
            if (voucher == null && code.Trim().ToUpper() == "ASUSFLASH30")
            {
                voucher = new Voucher
                {
                    Code = "ASUSFLASH30",
                    DiscountType = "Percent",
                    DiscountValue = 30,
                    MaxDiscountAmount = 5000000,
                    MinOrderAmount = 0,
                    IsActive = true,
                    StartDate = DateTime.Now.AddDays(-30),
                    ExpiryDate = DateTime.Now.AddDays(365)
                };
            }
            else if (voucher == null && (code.Trim().ToUpper() == "ASUSVIP1M" || code.Trim().ToUpper() == "VIP1000K"))
            {
                voucher = new Voucher
                {
                    Code = "ASUSVIP1M",
                    Description = "Voucher VIP Club - Giảm ngay 1.000.000đ cho đơn từ 5tr",
                    DiscountType = "Amount",
                    DiscountValue = 1000000,
                    MinOrderAmount = 5000000,
                    IsActive = true,
                    StartDate = DateTime.Now.AddDays(-30),
                    ExpiryDate = DateTime.Now.AddDays(365)
                };
            }

            if (voucher == null || !voucher.IsActive)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị khóa." });

            var now = DateTime.Now;
            if (now < voucher.StartDate || now > voucher.ExpiryDate)
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn hoặc chưa có hiệu lực." });

            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });

            if (cartTotal < voucher.MinOrderAmount)
                return Json(new { success = false, message = $"Đơn hàng cần tối thiểu {voucher.MinOrderAmount:N0}₫ để áp dụng mã này." });

            decimal discount = voucher.DiscountType == "Percent"
                ? cartTotal * (voucher.DiscountValue / 100m)
                : voucher.DiscountValue;

            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                discount = voucher.MaxDiscountAmount.Value;

            if (discount > cartTotal) discount = cartTotal;

            HttpContext.Session.SetString("VoucherCode", voucher.Code);
            HttpContext.Session.SetString("VoucherDiscount", discount.ToString("F0"));

            return Json(new
            {
                success = true,
                message = $"Áp dụng mã \"{voucher.Code}\" thành công!",
                discount,
                newTotal = cartTotal - discount
            });
        }

        [HttpPost]
        public IActionResult RemoveVoucher()
        {
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("VoucherDiscount");
            return Json(new { success = true });
        }

        // ─── CHECKOUT ───────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string customerName, string phone, string address,
            string email, string? note, string paymentMethod = "COD")
        {
            if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(address))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ họ tên, số điện thoại và địa chỉ.";
                return RedirectToAction("Index");
            }

            string sessionId = GetCartSessionId();
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            if (!cartItems.Any()) { TempData["ErrorMessage"] = "Giỏ hàng trống!"; return RedirectToAction("Index"); }

            // ── Kiểm tra tồn kho trước khi cho đặt hàng — chặn nếu hết hàng hoặc không đủ số lượng ──
            var outOfStockMessages = new List<string>();
            foreach (var item in cartItems)
            {
                int availableStock = item.Variant != null ? item.Variant.Stock : (item.Product?.Quantity ?? 0);
                if (availableStock <= 0)
                {
                    outOfStockMessages.Add($"{item.Product?.Name} {(item.Variant != null ? $"({item.Variant.DisplayLabel})" : "")} đã hết hàng.");
                }
                else if (item.Quantity > availableStock)
                {
                    outOfStockMessages.Add($"{item.Product?.Name} {(item.Variant != null ? $"({item.Variant.DisplayLabel})" : "")} chỉ còn {availableStock} sản phẩm, bạn đang đặt {item.Quantity}.");
                }
            }
            if (outOfStockMessages.Any())
            {
                TempData["ErrorMessage"] = "Không thể đặt hàng vì: " + string.Join(" ", outOfStockMessages) + " Vui lòng cập nhật lại giỏ hàng.";
                return RedirectToAction("Index");
            }

            string payStatus = paymentMethod switch
            {
                "VNPay"        => "Pending",
                "Momo"         => "Pending",
                "BankTransfer" => "Pending",
                _              => "Unpaid"
            };

            decimal subTotal = cartItems.Sum(c => ((c.Product?.Price ?? 0) + (c.Variant?.PriceAdjust ?? 0)) * c.Quantity);

            // Áp dụng voucher đã lưu trong session (nếu có)
            string? voucherCode = HttpContext.Session.GetString("VoucherCode");
            decimal discountAmount = 0;
            Voucher? appliedVoucher = null;
            if (!string.IsNullOrEmpty(voucherCode))
            {
                appliedVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode);
                if (appliedVoucher != null && appliedVoucher.IsActive
                    && DateTime.Now <= appliedVoucher.ExpiryDate
                    && subTotal >= appliedVoucher.MinOrderAmount
                    && (!appliedVoucher.UsageLimit.HasValue || appliedVoucher.UsedCount < appliedVoucher.UsageLimit.Value))
                {
                    discountAmount = appliedVoucher.DiscountType == "Percent"
                        ? subTotal * (appliedVoucher.DiscountValue / 100m)
                        : appliedVoucher.DiscountValue;
                    if (appliedVoucher.MaxDiscountAmount.HasValue && discountAmount > appliedVoucher.MaxDiscountAmount.Value)
                        discountAmount = appliedVoucher.MaxDiscountAmount.Value;
                    if (discountAmount > subTotal) discountAmount = subTotal;
                }
                else
                {
                    voucherCode = null; // voucher không còn hợp lệ lúc checkout -> bỏ qua
                }
            }

            var order = new Order
            {
                CustomerName    = customerName,
                Phone           = phone,
                Address         = address,
                Email           = email ?? string.Empty,
                Note            = note,
                OrderDate       = DateTime.Now,
                Status          = "Pending",
                TotalAmount     = subTotal - discountAmount,
                PaymentMethod   = paymentMethod,
                PaymentStatus   = payStatus,
                VoucherCode     = voucherCode,
                DiscountAmount  = discountAmount
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null) order.UserId = user.Id;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var detail = new OrderDetail
                {
                    OrderId   = order.Id,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Quantity  = item.Quantity,
                    Price     = (item.Product?.Price ?? 0) + (item.Variant?.PriceAdjust ?? 0)
                };
                _context.OrderDetails.Add(detail);

                // Trừ stock của variant (nếu có), hoặc product
                int stockAfter;
                if (item.Variant != null)
                {
                    item.Variant.Stock = Math.Max(0, item.Variant.Stock - item.Quantity);
                    stockAfter = item.Variant.Stock;
                }
                else if (item.Product != null)
                {
                    item.Product.Quantity = Math.Max(0, item.Product.Quantity - item.Quantity);
                    stockAfter = item.Product.Quantity;
                }
                else stockAfter = 0;

                // Ghi nhật ký kho hàng — xuất kho do bán hàng
                _context.InventoryLogs.Add(new InventoryLog
                {
                    ProductId       = item.ProductId,
                    VariantId       = item.VariantId,
                    QuantityChange  = -item.Quantity,
                    StockAfter      = stockAfter,
                    Reason          = "Sale",
                    Note            = $"Bán hàng theo đơn #{order.Id}",
                    OrderId         = order.Id,
                    CreatedAt       = DateTime.Now
                });
            }

            // Lưu trước để lấy được Id của từng OrderDetail (cần cho việc gán serial number)
            await _context.SaveChangesAsync();

            // ── Gán serial number thật cho từng đơn vị đã bán (phục vụ tra cứu bảo hành) ──
            var savedDetailsForSerial = await _context.OrderDetails
                .Where(d => d.OrderId == order.Id)
                .ToListAsync();

            foreach (var detail in savedDetailsForSerial)
            {
                if (detail.VariantId == null) continue; // chỉ variant mới quản lý serial number

                var availableSerials = await _context.SerialNumbers
                    .Where(s => s.VariantId == detail.VariantId && s.Status == "Available")
                    .OrderBy(s => s.Id)
                    .Take(detail.Quantity)
                    .ToListAsync();

                foreach (var serial in availableSerials)
                {
                    serial.Status = "Sold";
                    serial.OrderDetailId = detail.Id;
                    serial.WarrantyEnd = DateTime.Now.AddMonths(24); // Bảo hành 24 tháng
                    serial.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            if (appliedVoucher != null && voucherCode != null)
            {
                appliedVoucher.UsedCount += 1;
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("VoucherDiscount");

            await _notificationService.NotifyAdminsAsync(
                "Đơn hàng mới",
                $"Đơn hàng #{order.Id} vừa được đặt bởi {customerName} — {order.TotalAmount.ToString("N0")}₫",
                "Order",
                $"/Admin/OrderDetail/{order.Id}"
            );

            if (paymentMethod == "VNPay")
            {
                var payUrl = _vnPay.CreatePaymentUrl(
                    order.Id, order.TotalAmount,
                    $"Thanh toan don hang #{order.Id} - ASUS Laptop Store");
                return Redirect(payUrl);
            }

            if (paymentMethod == "Momo")
            {
                var (success, momoPayUrl, message) = await _momo.CreatePaymentAsync(
                    order.Id, order.TotalAmount,
                    $"Thanh toan don hang #{order.Id} - ASUS Laptop Store");

                if (success)
                {
                    return Redirect(momoPayUrl);
                }

                // MoMo lỗi -> vẫn giữ đơn hàng (đã lưu), báo lỗi và để khách chọn lại phương thức khác
                order.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = $"Không thể khởi tạo thanh toán MoMo: {message}. Đơn hàng #{order.Id} đã được lưu, vui lòng vào 'Đơn hàng của tôi' để thanh toán lại hoặc chọn phương thức khác.";
                return RedirectToAction("MyOrders", "Account");
            }

            // Gửi email xác nhận đơn hàng (COD / BankTransfer)
            if (!string.IsNullOrWhiteSpace(order.Email))
            {
                var savedDetails = await _context.OrderDetails
                    .Include(d => d.Product)
                    .Include(d => d.Variant)
                    .Where(d => d.OrderId == order.Id)
                    .ToListAsync();
                _ = Task.Run(async () =>
                {
                    try { await _emailService.SendOrderConfirmationEmailAsync(order, savedDetails); }
                    catch { /* Không để lỗi email ảnh hưởng đến checkout */ }
                });
            }

            TempData["SuccessOrderId"]       = order.Id.ToString();
            TempData["SuccessOrderTotal"]    = order.TotalAmount.ToString("N0");
            TempData["SuccessPaymentMethod"] = order.PaymentMethod;
            return RedirectToAction("CheckoutSuccess");
        }

        // ─── VNPAY CALLBACK ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            if (!_vnPay.ValidateSignature(Request.Query, out var txnRef, out var responseCode))
            {
                TempData["ErrorMessage"] = "Chữ ký VNPay không hợp lệ. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }

            var orderId = int.Parse(txnRef.Split('_')[0]);
            var order   = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (responseCode == "00")
            {
                order.PaymentStatus = "Paid";
                order.Status        = "Processing";
                await _context.SaveChangesAsync();

                // Gửi email xác nhận sau thanh toán VNPay
                if (!string.IsNullOrWhiteSpace(order.Email))
                {
                    var savedDetails = await _context.OrderDetails
                        .Include(d => d.Product)
                        .Include(d => d.Variant)
                        .Where(d => d.OrderId == order.Id)
                        .ToListAsync();
                    _ = Task.Run(async () =>
                    {
                        try { await _emailService.SendOrderConfirmationEmailAsync(order, savedDetails); }
                        catch { /* Không để lỗi email ảnh hưởng đến flow */ }
                    });
                }

                TempData["SuccessOrderId"]       = order.Id.ToString();
                TempData["SuccessOrderTotal"]    = order.TotalAmount.ToString("N0");
                TempData["SuccessPaymentMethod"] = "VNPay";
                return RedirectToAction("CheckoutSuccess");
            }
            else
            {
                order.PaymentStatus = "Failed";
                await AsusLaptop.Services.OrderCancellationHelper.CancelAndRestoreStockAsync(
                    _context, order, $"VNPay báo lỗi/hủy (Mã: {responseCode})");
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = $"Thanh toán VNPay không thành công hoặc đã bị hủy (Mã lỗi: {responseCode}). Đơn hàng #{orderId} đã được hủy tự động.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ─── MOMO CALLBACK (khách được MoMo redirect trình duyệt về đây) ─────────
        [HttpGet]
        public async Task<IActionResult> MomoReturn()
        {
            if (!_momo.ValidateSignature(Request.Query, out var orderId, out var resultCode))
            {
                TempData["ErrorMessage"] = "Chữ ký MoMo không hợp lệ. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (resultCode == 0)
            {
                // Chỉ cập nhật nếu IPN (server-to-server) chưa xử lý trước đó
                if (order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Paid";
                    order.Status        = "Processing";
                    await _context.SaveChangesAsync();
                }

                if (!string.IsNullOrWhiteSpace(order.Email))
                {
                    var savedDetails = await _context.OrderDetails
                        .Include(d => d.Product)
                        .Include(d => d.Variant)
                        .Where(d => d.OrderId == order.Id)
                        .ToListAsync();
                    _ = Task.Run(async () =>
                    {
                        try { await _emailService.SendOrderConfirmationEmailAsync(order, savedDetails); }
                        catch { /* Không để lỗi email ảnh hưởng đến flow */ }
                    });
                }

                TempData["SuccessOrderId"]       = order.Id.ToString();
                TempData["SuccessOrderTotal"]    = order.TotalAmount.ToString("N0");
                TempData["SuccessPaymentMethod"] = "Momo";
                return RedirectToAction("CheckoutSuccess");
            }
            else
            {
                if (order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Failed";
                    await AsusLaptop.Services.OrderCancellationHelper.CancelAndRestoreStockAsync(
                        _context, order, $"MoMo báo lỗi/hủy (resultCode={resultCode})");
                }
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = $"Thanh toán MoMo không thành công hoặc đã bị hủy (resultCode={resultCode}). Đơn hàng #{orderId} đã được hủy tự động.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ─── MOMO IPN (server-to-server, đáng tin cậy hơn MomoReturn) ─────────────
        // MoMo gửi dữ liệu dưới dạng JSON trong body, KHÔNG phải query string.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MomoIpn()
        {
            Dictionary<string, string> data;
            try
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
                data = doc.RootElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.ValueKind == System.Text.Json.JsonValueKind.String
                        ? (p.Value.GetString() ?? "")
                        : p.Value.ToString());
            }
            catch
            {
                return Ok(); // body không hợp lệ -> trả 200 để MoMo không retry, bỏ qua
            }

            if (!_momo.ValidateSignature(data, out var orderId, out var resultCode))
                return Ok();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Ok();

            if (resultCode == 0 && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.Status        = "Processing";
                await _context.SaveChangesAsync();
            }
            else if (resultCode != 0 && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Failed";
                await AsusLaptop.Services.OrderCancellationHelper.CancelAndRestoreStockAsync(
                    _context, order, $"MoMo IPN báo lỗi/hủy (resultCode={resultCode})");
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        public IActionResult CheckoutSuccess()
        {
            if (TempData["SuccessOrderId"] == null) return RedirectToAction("Index", "Home");
            ViewBag.OrderId       = TempData["SuccessOrderId"];
            ViewBag.OrderTotal    = TempData["SuccessOrderTotal"];
            ViewBag.PaymentMethod = TempData["SuccessPaymentMethod"];
            return View();
        }
    }
}
