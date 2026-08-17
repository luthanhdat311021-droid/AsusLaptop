namespace AsusLaptop.Models
{
    public class FutureFitRequest
    {
        public int ProductId { get; set; }
        public string Scenario { get; set; } = "office";
        public int Years { get; set; } = 3;
    }
}
