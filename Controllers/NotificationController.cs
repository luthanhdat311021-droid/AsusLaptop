using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;

namespace AsusLaptop.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdminOrSub() => User.Identity?.IsAuthenticated == true &&
                                        (User.IsInRole("Admin") || User.IsInRole("SubAdmin"));

        // Danh sách thông báo của người dùng hiện tại (kèm thông báo broadcast cho admin nếu có quyền)
        [HttpGet]
        public async Task<IActionResult> List(int take = 15)
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { items = Array.Empty<object>(), unread = 0 });
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var query = _context.Notifications.Where(n => n.UserId == userId);
            if (IsAdminOrSub())
                query = _context.Notifications.Where(n => n.UserId == userId || n.UserId == null);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new {
                    n.Id, n.Title, n.Message, n.Type, n.IsRead, n.ActionUrl, n.CreatedAt
                })
                .ToListAsync();

            int unread = await query.CountAsync(n => !n.IsRead);

            return Json(new { items, unread });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { success = false });
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && (x.UserId == userId || (x.UserId == null && IsAdminOrSub())));
            if (n == null) return Json(new { success = false });

            n.IsRead = true;
            n.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { success = false });
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var query = IsAdminOrSub()
                ? _context.Notifications.Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                : _context.Notifications.Where(n => n.UserId == userId && !n.IsRead);

            var list = await query.ToListAsync();
            foreach (var n in list) { n.IsRead = true; n.ReadAt = DateTime.Now; }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
