using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Product lifecycle estimator: evaluates future suitability using only the listed configuration.
    /// It is a buying aid, not a performance guarantee.
    /// </summary>
    public class FutureFitAiService
    {
        public object Analyze(Product product, string? scenario, int years)
        {
            scenario = (scenario ?? "office").ToLowerInvariant();
            years = Math.Clamp(years, 1, 5);
            var score = 52 + ScoreHardware(product, scenario);
            score -= Math.Max(0, years - 1) * (scenario == "gaming" || scenario == "creative" ? 8 : 5);
            score = Math.Clamp(score, 35, 98);

            var bottleneck = GetBottleneck(product, scenario);
            var upgrade = product.RAM.Contains("8")
                ? "Ưu tiên nâng RAM lên tối thiểu 16GB khi khối lượng công việc tăng."
                : product.Storage.Contains("256")
                    ? "Cân nhắc nâng SSD khi dung lượng lưu trữ bắt đầu thiếu."
                    : "Theo dõi RAM và dung lượng SSD theo nhu cầu thực tế; đây thường là hai hạng mục dễ nâng cấp nhất.";

            var milestone = score >= 75
                ? $"Tự tin cho nhu cầu đã chọn trong khoảng {years} năm, nếu phần mềm không tăng yêu cầu đột biến."
                : score >= 58
                    ? "Phù hợp trong giai đoạn đầu; nên lên kế hoạch nâng cấp hoặc giảm tải tác vụ nặng sau 1–2 năm."
                    : "Nên cân nhắc cấu hình cao hơn nếu bạn cần duy trì tác vụ nặng trong nhiều năm.";

            return new
            {
                score,
                label = score >= 75 ? "Sẵn sàng dài hạn" : score >= 58 ? "Phù hợp có điều kiện" : "Cần cân nhắc nâng cấu hình",
                milestone,
                bottleneck,
                upgrade,
                disclaimer = "Dự báo dựa trên cấu hình đang niêm yết và nhu cầu bạn chọn; không thay thế kiểm tra khả năng nâng cấp thực tế của từng phiên bản."
            };
        }

        private static int ScoreHardware(Product p, string scenario)
        {
            var text = $"{p.CPU} {p.RAM} {p.GPU} {p.Series}".ToLowerInvariant();
            var score = 0;
            if (text.Contains("i7") || text.Contains("i9") || text.Contains("ryzen 7") || text.Contains("ryzen 9") || text.Contains("ultra 7")) score += 12;
            else if (text.Contains("i5") || text.Contains("ryzen 5") || text.Contains("ultra 5")) score += 7;
            if (p.RAM.Contains("32") || p.RAM.Contains("64")) score += 14;
            else if (p.RAM.Contains("16")) score += 9;
            else if (p.RAM.Contains("8")) score += 2;
            if (scenario is "gaming" or "creative")
            {
                if (text.Contains("4090") || text.Contains("4080") || text.Contains("4070")) score += 18;
                else if (text.Contains("4060") || text.Contains("4050")) score += 13;
                else if (text.Contains("rtx")) score += 8;
                else score -= 8;
            }
            else if (scenario == "programming" && p.Storage.Contains("1TB")) score += 5;
            return score;
        }

        private static string GetBottleneck(Product p, string scenario)
        {
            if (p.RAM.Contains("8")) return "RAM 8GB có thể là điểm nghẽn khi chạy nhiều ứng dụng hoặc dự án lớn.";
            if ((scenario == "gaming" || scenario == "creative") && !p.GPU.Contains("RTX")) return "GPU là yếu tố cần kiểm tra kỹ nếu bạn chơi game mới hoặc render đồ họa.";
            if (p.Storage.Contains("256")) return "SSD 256GB có thể nhanh đầy khi lưu game, video hoặc dự án lớn.";
            return "Chưa thấy điểm nghẽn rõ rệt từ cấu hình niêm yết; hãy theo dõi yêu cầu của phần mềm bạn dùng.";
        }
    }
}
