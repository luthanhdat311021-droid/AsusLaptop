namespace AsusLaptop.Models
{
    public class FlashSaleItemViewModel
    {
        public Product Product { get; set; } = null!;
        public int DiscountPercent { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal FlashPrice { get; set; }
        public int SoldPercent { get; set; }
    }

    public class FlashSaleSectionViewModel
    {
        public bool IsActiveNow { get; set; }
        public string SlotName { get; set; } = "";
        public string EndTimeIso { get; set; } = "";
        public List<FlashSaleItemViewModel> Items { get; set; } = new();
    }
}
