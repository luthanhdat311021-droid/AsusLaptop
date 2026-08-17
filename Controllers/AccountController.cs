using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Helpers;
using AsusLaptop.Models;
using AsusLaptop.Services;

namespace AsusLaptop.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash, out bool needsRehash))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            if (needsRehash)
            {
                user.PasswordHash = PasswordHelper.HashPassword(password);
                await _context.SaveChangesAsync();
            }

            await SignInUser(user);
            MergeGuestCart(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return (user.Role == "Admin" || user.Role == "SubAdmin")
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ===== FACE ID PAGE =====
        [HttpGet]
        public async Task<IActionResult> FaceId()
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirstValue("UserId")!);
            var user = await _context.Users.FindAsync(userId);
            ViewBag.HasFace = user != null && !string.IsNullOrEmpty(user.FaceToken);
            return View();
        }

        // ===== GOOGLE LOGIN =====
        [HttpGet]
        public IActionResult LoginWithGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            // Lấy thông tin từ Google
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Đăng nhập Google thất bại.";
                return RedirectToAction("Login");
            }

            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var fullName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "";
            var avatar = result.Principal.FindFirstValue("urn:google:picture") ?? "";

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không lấy được email từ Google.";
                return RedirectToAction("Login");
            }

            // Tìm user đã có hoặc tạo mới
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId || u.Email == email);

            if (user == null)
            {
                // Tạo tài khoản mới từ Google
                var username = email.Split('@')[0] + "_" + googleId.Substring(0, 6);
                user = new User
                {
                    Username = username,
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()), // random password
                    Email = email,
                    FullName = fullName,
                    Phone = "",
                    Role = "Customer",
                    GoogleId = googleId,
                    AvatarUrl = avatar
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Cập nhật GoogleId nếu chưa có
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    user.AvatarUrl = avatar;
                    await _context.SaveChangesAsync();
                }
            }

            await SignInUser(user);
            MergeGuestCart(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return (user.Role == "Admin" || user.Role == "SubAdmin")
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ===== FACEBOOK LOGIN =====
        [HttpGet]
        public IActionResult LoginWithFacebook(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("FacebookCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> FacebookCallback(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

            // Thêm đoạn này để xử lý khi user bấm Hủy
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Đăng nhập Facebook bị hủy hoặc thất bại.";
                return RedirectToAction("Login");
            }

            var facebookId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var fullName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

            // Lấy avatar từ Facebook Graph API
            var avatar = $"https://graph.facebook.com/{facebookId}/picture?type=large";

            // Tìm user đã có hoặc tạo mới
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.FacebookId == facebookId ||
                (!string.IsNullOrEmpty(email) && u.Email == email));

            if (user == null)
            {
                var username = string.IsNullOrEmpty(email)
                    ? "fb_" + facebookId.Substring(0, 8)
                    : email.Split('@')[0] + "_fb";

                // Tránh trùng username
                var baseUsername = username;
                int counter = 1;
                while (_context.Users.Any(u => u.Username == username))
                    username = baseUsername + counter++;

                user = new User
                {
                    Username = username,
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                    Email = email,
                    FullName = fullName,
                    Phone = "",
                    Role = "Customer",
                    FacebookId = facebookId,
                    AvatarUrl = avatar
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (string.IsNullOrEmpty(user.FacebookId))
                {
                    user.FacebookId = facebookId;
                    user.AvatarUrl = avatar;
                    await _context.SaveChangesAsync();
                }
            }

            await SignInUser(user);
            MergeGuestCart(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return (user.Role == "Admin" || user.Role == "SubAdmin")
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ===== HELPER: Sign in =====
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id.ToString()),
                new Claim("AvatarUrl", user.AvatarUrl ?? ""),
                new Claim("Phone", user.Phone ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });
        }

        // ===== HELPER: Merge guest cart =====
        private void MergeGuestCart(User user)
        {
            string tempId = HttpContext.Session.Id;
            var tempCart = _context.CartItems.Where(c => c.SessionId == tempId).ToList();
            if (tempCart.Any())
            {
                foreach (var item in tempCart)
                {
                    var existing = _context.CartItems.FirstOrDefault(c => c.SessionId == user.Username && c.ProductId == item.ProductId);
                    if (existing != null) { existing.Quantity += item.Quantity; _context.CartItems.Remove(item); }
                    else item.SessionId = user.Username;
                }
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string password, string confirmPassword, string email, string fullName, string phone)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin bắt buộc.");
                return View();
            }
            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
                return View();
            }
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("username", "Tên đăng nhập đã tồn tại.");
                return View();
            }
            if (_context.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("email", "Email đã được sử dụng.");
                return View();
            }

            _context.Users.Add(new User
            {
                Username = username,
                PasswordHash = PasswordHelper.HashPassword(password),
                Email = email,
                FullName = string.IsNullOrEmpty(fullName) ? username : fullName,
                Phone = phone ?? string.Empty,
                Role = "Customer"
            });
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string fullName, string email, string phone)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // ══════════════ SỔ ĐỊA CHỈ GIAO HÀNG ══════════════
        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.User = user;
            return View(addresses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddress model)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            model.Id = 0;
            model.UserId = userId;
            model.CreatedAt = DateTime.Now;

            // Nếu đây là địa chỉ đầu tiên hoặc người dùng chọn đặt mặc định
            bool hasAny = await _context.UserAddresses.AnyAsync(a => a.UserId == userId);
            if (!hasAny) model.IsDefault = true;

            if (model.IsDefault)
            {
                var others = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
            }

            _context.UserAddresses.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm địa chỉ mới!";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(UserAddress model)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var addr = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);
            if (addr == null) { TempData["ErrorMessage"] = "Không tìm thấy địa chỉ!"; return RedirectToAction(nameof(Addresses)); }

            addr.RecipientName = model.RecipientName;
            addr.Phone = model.Phone;
            addr.AddressLine = model.AddressLine;
            addr.Ward = model.Ward;
            addr.District = model.District;
            addr.City = model.City;
            addr.Label = model.Label;

            if (model.IsDefault && !addr.IsDefault)
            {
                var others = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
                addr.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật địa chỉ!";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var addr = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (addr != null)
            {
                bool wasDefault = addr.IsDefault;
                _context.UserAddresses.Remove(addr);
                await _context.SaveChangesAsync();

                // Nếu xoá địa chỉ mặc định, gán mặc định cho địa chỉ khác (nếu còn)
                if (wasDefault)
                {
                    var next = await _context.UserAddresses.Where(a => a.UserId == userId).FirstOrDefaultAsync();
                    if (next != null) { next.IsDefault = true; await _context.SaveChangesAsync(); }
                }
                TempData["SuccessMessage"] = "Đã xoá địa chỉ!";
            }
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var addresses = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
            foreach (var a in addresses) a.IsDefault = (a.Id == id);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đặt làm địa chỉ mặc định!";
            return RedirectToAction(nameof(Addresses));
        }

        // API nhẹ để trang Giỏ hàng/Checkout lấy danh sách địa chỉ đã lưu (AJAX)
        [HttpGet]
        public async Task<IActionResult> AddressesJson()
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new List<object>());
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new {
                    a.Id, a.RecipientName, a.Phone, a.AddressLine, a.Ward, a.District, a.City, a.IsDefault, a.Label
                })
                .ToListAsync();
            return Json(addresses);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ thông tin.");
                return View();
            }
            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
                return View();
            }
            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("confirmNewPassword", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("currentPassword", "Mật khẩu hiện tại không đúng!");
                return View();
            }
            if (currentPassword == newPassword)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải khác mật khẩu hiện tại.");
                return View();
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", new { returnUrl = "/Account/MyOrders" });
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) { TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!"; return RedirectToAction(nameof(MyOrders)); }

            var orderDetailIds = order.OrderDetails.Select(od => od.Id).ToList();
            var serials = await _context.SerialNumbers
                .Where(s => s.OrderDetailId != null && orderDetailIds.Contains(s.OrderDetailId.Value))
                .ToListAsync();
            // Map: OrderDetailId -> danh sách serial (1 sản phẩm có thể mua số lượng > 1 => nhiều serial)
            ViewBag.SerialsByDetail = serials
                .GroupBy(s => s.OrderDetailId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(s => s.SerialNo).ToList());

            return View(order);
        }

        // ── In hoá đơn ───────────────────────────────────────────
        // Chủ đơn hàng hoặc Admin/SubAdmin đều có thể xem & in
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");

            bool isStaff = User.IsInRole("Admin") || User.IsInRole("SubAdmin");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.Id == id && (isStaff || o.UserId == userId));

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem hoá đơn này!";
                return RedirectToAction(nameof(MyOrders));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) { TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!"; return RedirectToAction(nameof(MyOrders)); }
            if (order.Status != "Pending") { TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý!"; return RedirectToAction(nameof(MyOrders)); }

            await OrderCancellationHelper.CancelAndRestoreStockAsync(_context, order, "Khách tự hủy đơn");
            if (order.PaymentStatus == "Unpaid" || order.PaymentStatus == "Pending") order.PaymentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{orderId} thành công!";
            return RedirectToAction(nameof(MyOrders));
        }

        // ===== QUÊN MẬT KHẨU - Bước 1: Nhập email =====
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("otp-policy")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập email.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Bảo mật: không tiết lộ email có tồn tại hay không
                TempData["SuccessMessage"] = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction(nameof(VerifyOtp));
            }

            // Tạo OTP 6 chữ số
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.Now.AddMinutes(10);

            // Lưu OTP vào Session
            HttpContext.Session.SetString("OtpCode", otp);
            HttpContext.Session.SetString("OtpEmail", email);
            HttpContext.Session.SetString("OtpExpiry", expiry.ToString("o"));

            try
            {
                await _emailService.SendOtpEmailAsync(email, user.FullName, otp);
                TempData["SuccessMessage"] = $"Mã OTP đã được gửi đến {email}. Vui lòng kiểm tra hộp thư (kể cả thư mục Spam).";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Gửi email thất bại: {ex.Message}. Vui lòng kiểm tra cấu hình SMTP.");
                return View();
            }

            return RedirectToAction(nameof(VerifyOtp));
        }

        // ===== QUÊN MẬT KHẨU - Bước 2: Nhập OTP =====
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.Email = HttpContext.Session.GetString("OtpEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("otp-policy")]
        public IActionResult VerifyOtp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("OtpCode");
            var savedEmail = HttpContext.Session.GetString("OtpEmail");
            var savedExpiryStr = HttpContext.Session.GetString("OtpExpiry");

            ViewBag.Email = savedEmail;

            if (string.IsNullOrEmpty(savedOtp) || string.IsNullOrEmpty(savedEmail))
            {
                ModelState.AddModelError(string.Empty, "Phiên làm việc đã hết hạn. Vui lòng thực hiện lại từ đầu.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(otp))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập mã OTP.");
                return View();
            }

            if (DateTime.TryParse(savedExpiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry) && DateTime.Now > expiry)
            {
                HttpContext.Session.Remove("OtpCode");
                ModelState.AddModelError(string.Empty, "Mã OTP đã hết hạn (10 phút). Vui lòng gửi lại mã mới.");
                return View();
            }

            if (otp.Trim() != savedOtp)
            {
                ModelState.AddModelError(string.Empty, "Mã OTP không đúng. Vui lòng kiểm tra lại.");
                return View();
            }

            // OTP đúng → đánh dấu đã xác thực, xóa OTP khỏi session
            HttpContext.Session.Remove("OtpCode");
            HttpContext.Session.SetString("OtpVerified", "true");

            return RedirectToAction(nameof(ResetPassword));
        }

        // ===== QUÊN MẬT KHẨU - Bước 3: Đặt mật khẩu mới =====
        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString("OtpVerified") != "true")
                return RedirectToAction(nameof(ForgotPassword));

            ViewBag.Email = HttpContext.Session.GetString("OtpEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            if (HttpContext.Session.GetString("OtpVerified") != "true")
                return RedirectToAction(nameof(ForgotPassword));

            var email = HttpContext.Session.GetString("OtpEmail");
            ViewBag.Email = email;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng điền đầy đủ thông tin.");
                return View();
            }

            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản. Vui lòng thực hiện lại.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Xóa toàn bộ session liên quan
            HttpContext.Session.Remove("OtpVerified");
            HttpContext.Session.Remove("OtpEmail");
            HttpContext.Session.Remove("OtpExpiry");

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => View();
    }
}
