namespace AsusLaptop.Models
{
    public class ProductAutoFillRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ProductAutoFillResponse
    {
        public string Brand { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string CPU { get; set; } = string.Empty;
        public string RAM { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string GPU { get; set; } = string.Empty;
        public string ScreenSize { get; set; } = string.Empty;
        public string ScreenResolution { get; set; } = string.Empty;
        public string Battery { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string OS { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool FromCatalog { get; set; }
        public string Notice { get; set; } = string.Empty;
    }
}
