namespace AsusLaptop.Services
{
    public class WebsiteAutomationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WebsiteAutomationStore _store;
        private readonly ILogger<WebsiteAutomationService> _logger;

        public WebsiteAutomationService(
            IServiceScopeFactory scopeFactory,
            WebsiteAutomationStore store,
            ILogger<WebsiteAutomationService> logger)
        {
            _scopeFactory = scopeFactory;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = _store.GetSettings();
                var interval = Math.Clamp(settings.CheckIntervalMinutes, 1, 60);

                if (settings.Enabled)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var runner = scope.ServiceProvider.GetRequiredService<WebsiteAutomationRunner>();
                        await runner.RunCycleAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "WebsiteAutomationService cycle failed.");
                        _store.AddLog("System", "Lỗi chu kỳ tự động: " + ex.Message, "Error");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
        }
    }
}
