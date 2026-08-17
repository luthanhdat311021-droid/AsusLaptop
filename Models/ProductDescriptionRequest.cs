namespace AsusLaptop.Models
{
    public class ProductDescriptionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CPU { get; set; } = string.Empty;
        public string RAM { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string GPU { get; set; } = string.Empty;
        public string ScreenSize { get; set; } = string.Empty;
        public string ScreenResolution { get; set; } = string.Empty;
        public string Battery { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string OS { get; set; } = string.Empty;
    }
}
