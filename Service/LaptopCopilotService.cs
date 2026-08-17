using System.Globalization;
using System.Text.RegularExpressions;
using AsusLaptop.Data;
using AsusLaptop.Models;
using Microsoft.EntityFrameworkCore;

namespace AsusLaptop.Services
{
    public class LaptopCopilotService
    {
        private readonly ApplicationDbContext _context;
        public LaptopCopilotService(ApplicationDbContext context) => _context = context;

        public async Task<CopilotResponse> RecommendAsync(string? message)
        {
            var query = (message ?? string.Empty).ToLowerInvariant();
            var profile = Analyze(query);
            var products = await _context.Products.AsNoTracking().Where(p => p.Quantity > 0).ToListAsync();

            var ranked = products.Select(p => new { Product = p, Score = Score(p, profile) })
                .OrderByDescending(x => x.Score).ThenByDescending(x => x.Product.ViewCount).Take(3).ToList();

            return new CopilotResponse
            {
                Summary = profile.Budget > 0
                    ? $"Đã tìm máy cho nhu cầu {profile.UsageLabel.ToLowerInvariant()} trong tầm {profile.Budget / 1_000_000m:0.#} triệu."
                    : $"Đã ưu tiên laptop phù hợp cho nhu cầu {profile.UsageLabel.ToLowerInvariant()} của bạn.",
                DetectedNeeds = profile.Needs,
                Recommendations = ranked.Select(x => ToRecommendation(x.Product, x.Score, profile)).ToList()
            };
        }

        private static CopilotRecommendation ToRecommendation(Product p, int score, Profile profile)
        {
            var strengths = new List<string>();
            if (profile.Gaming && Has(p, "ROG", "TUF", "RTX")) strengths.Add("Đáp ứng tốt nhu cầu gaming");
            if (profile.Creative && Has(p, "ProArt", "OLED", "RTX", "32")) strengths.Add("Phù hợp đồ họa và sáng tạo");
            if (profile.Office && Has(p, "ZenBook", "VivoBook", "1.")) strengths.Add("Cân bằng cho học tập, văn phòng");
            if (profile.Light && Has(p, "1.", "ZenBook")) strengths.Add("Thiết kế gọn nhẹ, dễ mang theo");
            if (profile.Display && Has(p, "OLED", "2.8K", "4K", "240Hz")) strengths.Add("Màn hình là điểm mạnh");
            if (p.RAM.Contains("16") || p.RAM.Contains("32")) strengths.Add($"{p.RAM} RAM hỗ trợ đa nhiệm");
            if (!strengths.Any()) strengths.Add("Cấu hình và giá bán cân đối");

            var tradeoff = p.Weight.Contains("2") || p.Weight.Contains("3")
                ? "Đánh đổi: hiệu năng cao nhưng trọng lượng có thể hơi lớn."
                : p.GPU.Contains("RTX") && p.Price >= 30_000_000
                    ? "Đánh đổi: cấu hình mạnh đi kèm mức giá cao hơn."
                    : "Đánh đổi: nên kiểm tra kỹ GPU nếu bạn làm đồ họa hoặc chơi game nặng.";

            return new CopilotRecommendation
            {
                Id = p.Id, Name = p.Name, Series = p.Series, ImageUrl = p.ImageUrl,
                Price = p.Price.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "₫",
                MatchPercent = Math.Min(99, Math.Max(60, score)), Strengths = strengths.Take(2).ToList(),
                Tradeoff = tradeoff,
                Specs = string.Join(" · ", new[] { p.CPU, p.RAM, p.GPU }.Where(x => !string.IsNullOrWhiteSpace(x)))
            };
        }

        private static int Score(Product p, Profile profile)
        {
            var score = 55;
            if (profile.Gaming && Has(p, "ROG", "TUF", "RTX")) score += 25;
            if (profile.Creative && Has(p, "ProArt", "OLED", "RTX", "32")) score += 24;
            if (profile.Office && Has(p, "ZenBook", "VivoBook", "ExpertBook")) score += 22;
            if (profile.Light && Has(p, "1.", "ZenBook")) score += 12;
            if (profile.Display && Has(p, "OLED", "2.8K", "4K", "240Hz")) score += 12;
            if (profile.Budget > 0)
            {
                if (p.Price <= profile.Budget) score += 18;
                else if (p.Price <= profile.Budget * 1.12m) score += 5;
                else score -= 14;
            }
            return score + Math.Min(5, p.ViewCount / 20);
        }

        private static bool Has(Product p, params string[] values)
        {
            var text = $"{p.Series} {p.CPU} {p.RAM} {p.GPU} {p.ScreenResolution} {p.Weight}".ToLowerInvariant();
            return values.Any(value => text.Contains(value.ToLowerInvariant()));
        }

        private static Profile Analyze(string input)
        {
            var profile = new Profile
            {
                Gaming = Contains(input, "game", "gaming", "esport", "valorant", "gta"),
                Creative = Contains(input, "đồ họa", "thiet ke", "thiết kế", "edit", "render", "video", "photoshop"),
                Office = Contains(input, "văn phòng", "van phong", "học", "sinh viên", "office", "code", "lập trình"),
                Light = Contains(input, "nhẹ", "mong", "mỏng", "di chuyển", "mang theo"),
                Display = Contains(input, "màn", "oled", "hiển thị", "màu", "240hz", "4k")
            };
            profile.Budget = ParseBudget(input);
            if (!profile.Gaming && !profile.Creative && !profile.Office) profile.Office = true;
            profile.UsageLabel = profile.Gaming ? "Gaming" : profile.Creative ? "Đồ họa & sáng tạo" : "Học tập & văn phòng";
            if (profile.Gaming) profile.Needs.Add("Gaming");
            if (profile.Creative) profile.Needs.Add("Đồ họa / sáng tạo");
            if (profile.Office) profile.Needs.Add("Học tập / văn phòng");
            if (profile.Light) profile.Needs.Add("Gọn nhẹ");
            if (profile.Display) profile.Needs.Add("Màn hình đẹp");
            if (profile.Budget > 0) profile.Needs.Add($"Ngân sách ≤ {profile.Budget / 1_000_000m:0.#} triệu");
            return profile;
        }

        private static decimal ParseBudget(string input)
        {
            var match = Regex.Match(input, @"(?<!\d)(\d{1,3}(?:[\.,]\d+)?)\s*(triệu|tr|m)(?![a-z])", RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var million)) return million * 1_000_000m;
            return 0;
        }
        private static bool Contains(string source, params string[] values) => values.Any(source.Contains);

        private sealed class Profile
        {
            public bool Gaming { get; set; } public bool Creative { get; set; } public bool Office { get; set; }
            public bool Light { get; set; } public bool Display { get; set; } public decimal Budget { get; set; }
            public string UsageLabel { get; set; } = string.Empty; public List<string> Needs { get; } = new();
        }
    }
}
