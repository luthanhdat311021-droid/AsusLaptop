using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Controllers
{
    public class VoucherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VoucherController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdminOrSub() =>
            User.Identity?.IsAuthenticated == true && (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));

        public async Task<IActionResult> Manage()
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            var vouchers = await _context.Vouchers.OrderByDescending(v => v.Id).ToListAsync();
            return View(vouchers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            model.Code = model.Code.Trim().ToUpper();
            if (await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
            {
                TempData["ErrorMessage"] = "Mã voucher này đã tồn tại.";
                return RedirectToAction("Manage");
            }

            _context.Vouchers.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tạo voucher \"{model.Code}\".";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Voucher model)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var voucher = await _context.Vouchers.FindAsync(model.Id);
            if (voucher == null) return NotFound();

            voucher.Code              = model.Code.Trim().ToUpper();
            voucher.Description       = model.Description;
            voucher.DiscountType      = model.DiscountType;
            voucher.DiscountValue     = model.DiscountValue;
            voucher.MaxDiscountAmount = model.MaxDiscountAmount;
            voucher.MinOrderAmount    = model.MinOrderAmount;
            voucher.StartDate         = model.StartDate;
            voucher.ExpiryDate        = model.ExpiryDate;
            voucher.UsageLimit        = model.UsageLimit;
            voucher.IsActive          = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật voucher.";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            if (!IsAdminOrSub()) return RedirectToAction("Login", "Account");
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                voucher.IsActive = !voucher.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Manage");
        }

        [HttpPost]
        public async Task<IActionResult> SubscribeVip([FromBody] VipSubscribeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                return Json(new { success = false, message = "Vui lòng nhập địa chỉ email hợp lệ." });
            }

            var email = request.Email.Trim();
            var voucherCode = "ASUSVIP1M";

            var existingVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode);
            if (existingVoucher == null)
            {
                existingVoucher = new Voucher
                {
                    Code = voucherCode,
                    Description = "Voucher VIP Club - Giảm ngay 1.000.000đ cho đơn từ 5tr",
                    DiscountType = "Amount",
                    DiscountValue = 1000000,
                    MinOrderAmount = 5000000,
                    StartDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddDays(365),
                    IsActive = true
                };
                _context.Vouchers.Add(existingVoucher);
                await _context.SaveChangesAsync();
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    var notif = new Notification
                    {
                        UserId = user.Id,
                        Title = "👑 ĐÃ GIA NHẬP ASUS VIP CLUB!",
                        Message = $"Bạn đã đăng ký thành công VIP Club với email {email}. Mã giảm giá 1.000.000đ của bạn là: {voucherCode}",
                        Type = "Vip",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.Notifications.Add(notif);
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new
            {
                success = true,
                message = "Đăng ký VIP Club thành công!",
                voucherCode = voucherCode,
                discountAmount = "1.000.000₫",
                email = email
            });
        }
    }

    public class VipSubscribeRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
