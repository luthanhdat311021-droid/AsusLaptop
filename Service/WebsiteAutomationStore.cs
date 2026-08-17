using System.Text.Json;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    /// <summary>Lưu cấu hình + log tự động hóa (JSON file, không cần migration DB).</summary>
    public class WebsiteAutomationStore
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private WebsiteAutomationSnapshot _snapshot;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WebsiteAutomationStore(IWebHostEnvironment env)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "website-automation.json");
            _snapshot = LoadFromDisk();
        }

        public WebsiteAutomationSnapshot GetSnapshot()
        {
            lock (_lock) return Clone(_snapshot);
        }

        public WebsiteAutomationSettings GetSettings()
        {
            lock (_lock) return CloneSettings(_snapshot.Settings);
        }

        public void UpdateSettings(Action<WebsiteAutomationSettings> mutate)
        {
            lock (_lock)
            {
                mutate(_snapshot.Settings);
                SaveLocked();
            }
        }

        public void AddLog(string task, string message, string level = "Info")
        {
            lock (_lock)
            {
                _snapshot.RecentLogs.Insert(0, new AutomationLogEntry
                {
                    At = DateTime.Now,
                    Task = task,
                    Message = message,
                    Level = level
                });
                if (_snapshot.RecentLogs.Count > 80)
                    _snapshot.RecentLogs = _snapshot.RecentLogs.Take(80).ToList();
                SaveLocked();
            }
        }

        public void RecordRun(Action<WebsiteAutomationSnapshot> mutate)
        {
            lock (_lock)
            {
                _snapshot.LastRunAt = DateTime.Now;
                _snapshot.TotalRuns++;
                mutate(_snapshot);
                SaveLocked();
            }
        }

        public int GetSoldOverride(int productId, int defaultPercent)
        {
            lock (_lock)
            {
                return _snapshot.FlashSoldOverrides.TryGetValue(productId, out var v)
                    ? Math.Min(98, v)
                    : defaultPercent;
            }
        }

        public void BumpFlashSoldPercents(IEnumerable<int> productIds)
        {
            lock (_lock)
            {
                foreach (var id in productIds)
                {
                    var current = _snapshot.FlashSoldOverrides.TryGetValue(id, out var v) ? v : 70;
                    _snapshot.FlashSoldOverrides[id] = Math.Min(98, current + Random.Shared.Next(1, 4));
                }
                SaveLocked();
            }
        }

        public int NextMarqueeIndex(int total)
        {
            if (total <= 0) return 0;
            lock (_lock)
            {
                _snapshot.MarqueeIndex = (_snapshot.MarqueeIndex + 1) % total;
                SaveLocked();
                return _snapshot.MarqueeIndex;
            }
        }

        public int GetHeroSlideHint()
        {
            lock (_lock)
            {
                var minute = DateTime.Now.Minute;
                return minute % 5;
            }
        }

        private WebsiteAutomationSnapshot LoadFromDisk()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<WebsiteAutomationSnapshot>(json, JsonOpts)
                           ?? new WebsiteAutomationSnapshot();
                }
            }
            catch { /* ignore corrupt file */ }

            return new WebsiteAutomationSnapshot();
        }

        private void SaveLocked()
        {
            var json = JsonSerializer.Serialize(_snapshot, JsonOpts);
            File.WriteAllText(_filePath, json);
        }

        private static WebsiteAutomationSnapshot Clone(WebsiteAutomationSnapshot s) =>
            JsonSerializer.Deserialize<WebsiteAutomationSnapshot>(
                JsonSerializer.Serialize(s, JsonOpts), JsonOpts)!;

        private static WebsiteAutomationSettings CloneSettings(WebsiteAutomationSettings s) =>
            JsonSerializer.Deserialize<WebsiteAutomationSettings>(
                JsonSerializer.Serialize(s, JsonOpts), JsonOpts)!;
    }
}
