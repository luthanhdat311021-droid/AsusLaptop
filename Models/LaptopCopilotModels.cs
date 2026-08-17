namespace AsusLaptop.Models
{
    public class CopilotRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class CopilotResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> DetectedNeeds { get; set; } = new();
        public List<CopilotRecommendation> Recommendations { get; set; } = new();
    }

    public class CopilotRecommendation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public int MatchPercent { get; set; }
        public List<string> Strengths { get; set; } = new();
        public string Tradeoff { get; set; } = string.Empty;
        public string Specs { get; set; } = string.Empty;
    }
}
