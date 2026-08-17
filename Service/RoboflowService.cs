using System.Text.Json;

namespace AsusLaptop.Services
{
    public class RoboflowPrediction
    {
        public string Class { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class RoboflowResult
    {
        public List<RoboflowPrediction> Predictions { get; set; } = new();
    }

    public class RoboflowService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelEndpoint;

        public RoboflowService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Roboflow:ApiKey"] ?? "";
            _modelEndpoint = config["Roboflow:ModelEndpoint"] ?? "";
        }

        public async Task<RoboflowResult?> DetectFromImageAsync(IFormFile imageFile)
        {
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var url = $"{_modelEndpoint}?api_key={_apiKey}";

            var content = new StringContent(base64Image);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RoboflowResult>(json, options);
        }
    }
}