namespace AsusLaptop.Models
{
    public class WebsiteAutomationSettings
    {
        public bool Enabled { get; set; } = true;
        public bool AutoLowStockAlert { get; set; } = true;
        public bool AutoCleanStaleCarts { get; set; } = true;
        public bool AutoFlashSaleSync { get; set; } = true;
        public bool AutoMarqueeRotate { get; set; } = true;
        public int LowStockThreshold { get; set; } = 3;
        public int CheckIntervalMinutes { get; set; } = 3;
    }

    public class AutomationLogEntry
    {
        public DateTime At { get; set; } = DateTime.Now;
        public string Task { get; set; } = "";
        public string Message { get; set; } = "";
        public string Level { get; set; } = "Info";
    }

    public class WebsiteAutomationSnapshot
    {
        public DateTime? LastRunAt { get; set; }
        public int TotalRuns { get; set; }
        public int MarqueeIndex { get; set; }
        public Dictionary<int, int> FlashSoldOverrides { get; set; } = new();
        public List<AutomationLogEntry> RecentLogs { get; set; } = new();
        public WebsiteAutomationSettings Settings { get; set; } = new();
    }

    public class AutomationLiveStatusDto
    {
        public string ServerTimeIso { get; set; } = "";
        public bool FlashSaleActive { get; set; }
        public string FlashSaleEndIso { get; set; } = "";
        public string FlashSlotName { get; set; } = "";
        public List<string> MarqueeMessages { get; set; } = new();
        public int HeroSlideHint { get; set; }
        public List<AutomationLiveFlashItem> FlashItems { get; set; } = new();
    }

    public class AutomationLiveFlashItem
    {
        public int ProductId { get; set; }
        public int SoldPercent { get; set; }
    }
}
