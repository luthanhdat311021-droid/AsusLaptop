using System.Globalization;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>Creates a factual Vietnamese sales description using only administrator-entered specifications.</summary>
    public class ProductDescriptionAiService
    {
        public string Generate(ProductDescriptionRequest item)
        {
            var productName = string.IsNullOrWhiteSpace(item.Name) ? "Laptop ASUS" : item.Name.Trim();
            var series = string.IsNullOrWhiteSpace(item.Series) ? "" : $" thuộc dòng {item.Series.Trim()}";
            var specs = Join(item.CPU, item.RAM, item.Storage, item.GPU);
            var display = Join(item.ScreenSize, item.ScreenResolution);
            var mobility = Join(item.Weight.Length > 0 ? $"trọng lượng {item.Weight.Trim()}" : "", item.Battery.Length > 0 ? $"pin {item.Battery.Trim()}" : "");
            var usage = GetUsage(item);

            var lines = new List<string>
            {
                $"{productName}{series} là lựa chọn phù hợp cho nhu cầu {usage}."
            };
            if (!string.IsNullOrWhiteSpace(specs)) lines.Add($"Máy được trang bị {specs}, hỗ trợ xử lý công việc hằng ngày và đa nhiệm hiệu quả.");
            if (!string.IsNullOrWhiteSpace(display)) lines.Add($"Màn hình {display} mang đến không gian hiển thị rõ nét cho học tập, làm việc và giải trí.");
            if (!string.IsNullOrWhiteSpace(mobility)) lines.Add($"Thông số {mobility} giúp bạn chủ động hơn khi sử dụng và di chuyển.");
            if (!string.IsNullOrWhiteSpace(item.OS)) lines.Add($"Sản phẩm đi kèm {item.OS.Trim()}.");
            if (item.Price > 0) lines.Add($"Giá tham khảo: {item.Price.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}₫.");
            lines.Add("Vui lòng kiểm tra thông số, phiên bản và tình trạng hàng trước khi đặt mua.");
            return string.Join(" ", lines);
        }

        private static string GetUsage(ProductDescriptionRequest item)
        {
            var text = $"{item.Series} {item.GPU} {item.CPU}".ToLowerInvariant();
            if (text.Contains("rog") || text.Contains("tuf") || text.Contains("rtx")) return "chơi game và xử lý tác vụ nặng";
            if (text.Contains("proart")) return "thiết kế đồ họa và sáng tạo nội dung";
            if (text.Contains("zenbook") || text.Contains("vivobook") || text.Contains("expertbook")) return "học tập, văn phòng và làm việc linh hoạt";
            return "học tập, làm việc và giải trí";
        }

        private static string Join(params string[] values) => string.Join(", ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
