using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AsusLaptop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public class ChatRequest
        {
            public List<Message> Messages { get; set; } = new();
        }

        public class Message
        {
            public string Role { get; set; } = "";
            public string Content { get; set; } = "";
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequest request)
        {
            try
            {
                // FIX 1: Đọc đúng key từ "groq:ApiKey"
                var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                          ?? _configuration["groq:ApiKey"]
                          ?? "";

                if (string.IsNullOrWhiteSpace(apiKey))
                    return Ok(new { reply = "[LỖI] Chưa có API key." });

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // FIX 2: Thêm header Authorization cho Groq
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var systemPrompt = "Bạn là kỹ thuật viên hỗ trợ chuyên nghiệp của ASUS Laptop Store Việt Nam. Hãy trả lời bằng tiếng Việt, thân thiện, ngắn gọn và thực tế. Chuyên tư vấn về: lỗi phần cứng, phần mềm, driver, bảo hành, và các dòng laptop ASUS như ROG (gaming cao cấp), TUF Gaming (gaming tầm trung bền bỉ), ZenBook (mỏng nhẹ doanh nhân), VivoBook (phổ thông học sinh sinh viên), ProArt (đồ họa sáng tạo). Nếu vấn đề nghiêm trọng cần mang máy vào, hãy gợi ý khách gọi hotline 1800 1234. Không dùng markdown, chỉ dùng văn xuôi thuần.";

                // FIX 3: Dùng format OpenAI (Groq tương thích), không dùng format Gemini
                var messages = new List<object>();
                messages.Add(new { role = "system", content = systemPrompt });

                foreach (var msg in request.Messages)
                {
                    messages.Add(new {
                        role = msg.Role == "assistant" ? "assistant" : "user",
                        content = msg.Content
                    });
                }

                var payload = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages
                };

                var json = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://api.groq.com/openai/v1/chat/completions";

                HttpResponseMessage response;
                try
                {
                    response = await client.PostAsync(url, httpContent);
                }
                catch (Exception netEx)
                {
                    return Ok(new { reply = "[LỖI MẠNG] " + netEx.Message });
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Ok(new { reply = $"[LỖI {(int)response.StatusCode}] {responseBody}" });

                // FIX 4: Parse response theo format OpenAI, không phải Gemini
                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Không có phản hồi.";

                return Ok(new { reply = text });
            }
            catch (Exception ex)
            {
                return Ok(new { reply = "[EXCEPTION] " + ex.GetType().Name + ": " + ex.Message });
            }
        }
    }
}
