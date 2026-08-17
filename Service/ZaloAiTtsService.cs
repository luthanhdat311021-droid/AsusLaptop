using System.Text.Json;

namespace AsusLaptop.Services
{
    /// <summary>Server-side Zalo AI Text-to-Audio client. The API key never reaches the browser.</summary>
    public class ZaloAiTtsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ZaloAiTtsService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<byte[]?> SynthesizeAsync(string text)
        {
            var apiKey = Environment.GetEnvironmentVariable("ZALO_AI_API_KEY")
                ?? _configuration["ZaloAi:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.zalo.ai/v1/tts/synthesize");
            request.Headers.Add("apikey", apiKey);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["input"] = text,
                ["speaker_id"] = _configuration["ZaloAi:SpeakerId"] ?? "1",
                ["speed"] = _configuration["ZaloAi:Speed"] ?? "1.0"
            });

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data)) return null;

            string? audioUrl = data.ValueKind switch
            {
                JsonValueKind.String => data.GetString(),
                JsonValueKind.Object when data.TryGetProperty("url", out var url) => url.GetString(),
                JsonValueKind.Object when data.TryGetProperty("audio_url", out var audioUrlValue) => audioUrlValue.GetString(),
                _ => null
            };
            if (string.IsNullOrWhiteSpace(audioUrl)) return null;

            using var audioResponse = await client.GetAsync(audioUrl);
            return audioResponse.IsSuccessStatusCode ? await audioResponse.Content.ReadAsByteArrayAsync() : null;
        }
    }
}
