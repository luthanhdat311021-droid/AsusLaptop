namespace AsusLaptop.Models
{
    public class PersonalizedRecommendationViewModel
    {
        public string DisplayName { get; set; } = "bạn";
        public bool IsFaceRecognized { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<PersonalizedRecommendationItem> Items { get; set; } = new();
    }

    public class PersonalizedRecommendationItem
    {
        public Product Product { get; set; } = null!;
        public int MatchPercent { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
