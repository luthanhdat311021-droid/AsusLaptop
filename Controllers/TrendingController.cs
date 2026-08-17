using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;

namespace AsusLaptop.Controllers
{
    public class TrendingController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TrendingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Trending/Data?tab=views&period=year  => JSON
        [HttpGet]
        public async Task<IActionResult> Data(string tab = "views", string period = "year")
        {
            // Xác định khoảng thời gian
            DateTime from = period switch
            {
                "day"   => DateTime.Now.AddDays(-1),
                "week"  => DateTime.Now.AddDays(-7),
                "month" => DateTime.Now.AddMonths(-1),
                _       => DateTime.Now.AddYears(-1)
            };

            if (tab == "views")
            {
                // Top theo lượt xem
                var items = await _context.Products
                    .Where(p => p.ViewCount > 0)
                    .OrderByDescending(p => p.ViewCount)
                    .Take(10)
                    .Select(p => new {
                        p.Id,
                        p.Name,
                        p.ImageUrl,
                        p.Series,
                        Count = p.ViewCount
                    })
                    .ToListAsync();

                return Json(items);
            }
            else if (tab == "sell")
            {
                // Top theo lượt bán từ OrderDetails
                var items = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Include(od => od.Product)
                    .Where(od => od.Order != null && od.Order.OrderDate >= from)
                    .GroupBy(od => od.ProductId)
                    .Select(g => new {
                        Id       = g.Key,
                        Name     = g.First().Product!.Name,
                        ImageUrl = g.First().Product!.ImageUrl,
                        Series   = g.First().Product!.Series,
                        Count    = g.Sum(x => x.Quantity)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                return Json(items);
            }
            else // wishlist
            {
                // Top theo lượt yêu thích
                var items = await _context.WishlistItems
                    .Include(w => w.Product)
                    .GroupBy(w => w.ProductId)
                    .Select(g => new {
                        Id       = g.Key,
                        Name     = g.First().Product!.Name,
                        ImageUrl = g.First().Product!.ImageUrl,
                        Series   = g.First().Product!.Series,
                        Count    = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                return Json(items);
            }
        }

        // POST /Trending/TrackView/5  => tăng ViewCount
        [HttpPost]
        public async Task<IActionResult> TrackView(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.ViewCount++;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
