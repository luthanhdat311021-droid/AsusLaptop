using System.Text;
using System.Text.Json;
using AsusLaptop.Data;
using AsusLaptop.Models;
using Microsoft.EntityFrameworkCore;

namespace AsusLaptop.Services
{
    public class ProductAutoFillAiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProductAutoFillAiService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ProductAutoFillResponse?> GetDetailsAsync(string name)
        {
            var cleanName = name.Trim();
            var known = await _context.Products.AsNoTracking()
                .OrderByDescending(p => p.ViewCount)
                .FirstOrDefaultAsync(p => p.Name.ToLower() == cleanName.ToLower() || p.Name.ToLower().Contains(cleanName.ToLower()));
            if (known != null) return FromProduct(known, true, "Đã lấy cấu hình từ danh mục cửa hàng.");

            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? _configuration["groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var prompt = $"Trả về DUY NHẤT JSON cho laptop có tên: {cleanName}. Các trường bắt buộc: brand, series, cpu, ram, storage, gpu, screenSize, screenResolution, battery, weight, os, description. Dùng tiếng Việt. Nếu không chắc thông số, để chuỗi rỗng; tuyệt đối không bịa. description dài tối đa 450 ký tự, chỉ nêu thông số chắc chắn.";
            var payload = new { model = "llama-3.3-70b-versatile", messages = new[] { new { role = "system", content = "Bạn là hệ thống trích xuất thông số laptop. Chỉ xuất JSON hợp lệ, không markdown." }, new { role = "user", content = prompt } }, temperature = 0.1 };
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode) return null;

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var json = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(json)) return null;
            json = json.Trim().TrimStart('`').TrimEnd('`').Replace("json\n", "", StringComparison.OrdinalIgnoreCase).Trim();
            var result = JsonSerializer.Deserialize<ProductAutoFillResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result != null) result.Notice = "AI đã điền thông số dự kiến — hãy kiểm tra lại trước khi lưu.";
            return result;
        }

        private static ProductAutoFillResponse FromProduct(Product p, bool fromCatalog, string notice) => new()
        {
            Brand = p.Brand, Series = p.Series, CPU = p.CPU, RAM = p.RAM, Storage = p.Storage, GPU = p.GPU,
            ScreenSize = p.ScreenSize, ScreenResolution = p.ScreenResolution, Battery = p.Battery, Weight = p.Weight,
            OS = p.OS, Description = p.Description, FromCatalog = fromCatalog, Notice = notice
        };
    }
}
