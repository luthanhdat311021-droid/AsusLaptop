using AsusLaptop.Data;
using AsusLaptop.Models;
using Microsoft.EntityFrameworkCore;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Gợi ý sản phẩm từ các tín hiệu mà khách hàng chủ động tạo ra: wishlist và đơn hàng.
    /// Dữ liệu Face ID không được dùng để suy luận giới tính, tuổi hoặc bất kỳ thuộc tính nhạy cảm nào.
    /// </summary>
    public class PersonalizedRecommendationService
    {
        private readonly ApplicationDbContext _context;

        public PersonalizedRecommendationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PersonalizedRecommendationViewModel?> GetForUserAsync(int userId, bool isFaceRecognized)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var wishlistProducts = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.Product!)
                .ToListAsync();

            var purchasedProducts = await _context.OrderDetails
                .Where(od => od.Order!.UserId == userId)
                .Select(od => od.Product!)
                .ToListAsync();

            var referenceProducts = wishlistProducts.Concat(purchasedProducts).ToList();
            var alreadyOwnedIds = purchasedProducts.Select(p => p.Id).ToHashSet();
            var favoriteIds = wishlistProducts.Select(p => p.Id).ToHashSet();
            var candidates = await _context.Products
                .Where(p => p.Quantity > 0 && !alreadyOwnedIds.Contains(p.Id) && !favoriteIds.Contains(p.Id))
                .ToListAsync();

            var profile = new PersonalizedRecommendationViewModel
            {
                DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                IsFaceRecognized = isFaceRecognized,
                Reason = referenceProducts.Any()
                    ? "Dựa trên sản phẩm bạn yêu thích và các đơn hàng trước đây."
                    : "Chưa đủ lịch sử; hệ thống ưu tiên các mẫu đang được quan tâm nhiều."
            };

            profile.Items = candidates
                .Select(candidate => Score(candidate, referenceProducts))
                .OrderByDescending(item => item.MatchPercent)
                .ThenByDescending(item => item.Product.ViewCount)
                .ThenByDescending(item => item.Product.CreatedAt)
                .Take(3)
                .ToList();

            return profile;
        }

        private static PersonalizedRecommendationItem Score(Product candidate, List<Product> references)
        {
            var score = references.Any() ? 58 : 50;
            var reasons = new List<string>();

            foreach (var product in references)
            {
                if (Same(candidate.Series, product.Series)) { score += 14; reasons.Add($"cùng dòng {candidate.Series}"); }
                if (Same(candidate.Brand, product.Brand)) { score += 5; reasons.Add("cùng thương hiệu"); }
                if (Same(candidate.GPU, product.GPU)) { score += 7; reasons.Add("cấu hình đồ họa tương tự"); }
                if (Same(candidate.CPU, product.CPU)) { score += 5; reasons.Add("hiệu năng tương tự"); }
                if (Same(candidate.RAM, product.RAM)) { score += 3; reasons.Add("mức RAM phù hợp"); }

                if (product.Price > 0 && Math.Abs(candidate.Price - product.Price) / product.Price <= 0.20m)
                {
                    score += 8;
                    reasons.Add("trong tầm giá bạn quan tâm");
                }
            }

            if (candidate.OriginalPrice > candidate.Price) { score += 3; reasons.Add("đang có ưu đãi"); }
            if (candidate.ViewCount > 50) { score += 2; reasons.Add("được nhiều khách hàng xem"); }

            return new PersonalizedRecommendationItem
            {
                Product = candidate,
                MatchPercent = Math.Min(score, 99),
                Reason = reasons.FirstOrDefault() ?? "sản phẩm nổi bật phù hợp để bắt đầu"
            };
        }

        private static bool Same(string? left, string? right) =>
            !string.IsNullOrWhiteSpace(left) && string.Equals(left.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
