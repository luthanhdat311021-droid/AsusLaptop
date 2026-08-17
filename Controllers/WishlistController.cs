using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);
        }

        // Trang danh sách sản phẩm yêu thích
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var items = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // Bật/tắt yêu thích bằng AJAX — gọi từ nút tim trên Details/danh sách sản phẩm
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để dùng wishlist." });

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            bool added;
            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                added = false;
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = user.Id, ProductId = productId });
                added = true;
            }

            await _context.SaveChangesAsync();
            int count = await _context.WishlistItems.CountAsync(w => w.UserId == user.Id);
            return Json(new { success = true, added, wishlistCount = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
