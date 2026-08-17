using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AsusLaptop.Controllers
{
    public class FaceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== ĐĂNG KÝ KHUÔN MẶT =====
        [HttpPost]
        public async Task<IActionResult> RegisterFace([FromBody] FaceDescriptorRequest req)
        {
            if (!User.Identity!.IsAuthenticated)
                return Json(new { success = false, message = "Vui lòng đăng nhập trước." });

            if (req.Descriptor == null || req.Descriptor.Length != 128)
                return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ." });

            var userId = int.Parse(User.FindFirstValue("UserId")!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản." });

            // Lưu descriptor dưới dạng chuỗi
            user.FaceToken = string.Join(",", req.Descriptor);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đăng ký Face ID thành công!" });
        }

        // ===== ĐĂNG NHẬP BẰNG KHUÔN MẶT =====
        [HttpPost]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> LoginWithFace([FromBody] FaceDescriptorRequest req)
        {
            if (req.Descriptor == null || req.Descriptor.Length != 128)
                return Json(new { success = false, message = "Không phát hiện khuôn mặt. Hãy nhìn thẳng vào camera." });

            var usersWithFace = await _context.Users
                .Where(u => u.FaceToken != null && u.FaceToken != "")
                .ToListAsync();

            if (!usersWithFace.Any())
                return Json(new { success = false, message = "Chưa có tài khoản nào đăng ký Face ID." });

            User? matchedUser = null;
            double bestDistance = double.MaxValue;

            foreach (var u in usersWithFace)
            {
                try
                {
                    var stored = u.FaceToken!.Split(',').Select(double.Parse).ToArray();
                    var distance = EuclideanDistance(req.Descriptor, stored);
                    if (distance < 0.5 && distance < bestDistance)
                    {
                        bestDistance = distance;
                        matchedUser = u;
                    }
                }
                catch { continue; }
            }

            if (matchedUser == null)
                return Json(new { success = false, message = "Không nhận ra khuôn mặt. Thử lại hoặc đăng nhập bằng mật khẩu." });

            await SignInUser(matchedUser);

            var redirectUrl = (matchedUser.Role == "Admin" || matchedUser.Role == "SubAdmin")
                ? "/Admin/Dashboard"
                : "/Home/Index";

            return Json(new
            {
                success = true,
                message = $"Xin chào {matchedUser.FullName ?? matchedUser.Username}!",
                redirect = redirectUrl,
                confidence = Math.Round((1 - bestDistance) * 100, 1)
            });
        }

        // ===== XÓA FACE ID =====
        [HttpPost]
        public async Task<IActionResult> RemoveFace()
        {
            if (!User.Identity!.IsAuthenticated)
                return Json(new { success = false });

            var userId = int.Parse(User.FindFirstValue("UserId")!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false });

            user.FaceToken = null;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa Face ID." });
        }

        // ===== HELPERS =====
        private static double EuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Pow(a[i] - b[i], 2);
            return Math.Sqrt(sum);
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("AvatarUrl", user.AvatarUrl ?? ""),
                new Claim("LoginMethod", "FaceId")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
        }
    }

    public class FaceDescriptorRequest
    {
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }
}
