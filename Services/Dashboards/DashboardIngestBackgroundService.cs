namespace ONERI.Services.Dashboards;

public class DashboardIngestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardIngestBackgroundService> _logger;

    public DashboardIngestBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardIngestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshNow(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshNow(stoppingToken);
        }
    }

    private async Task RefreshNow(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IDashboardIngestionService>();
            await ingestionService.RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // no-op
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard background ingest çalışırken hata oluştu.");
        }
    }
}
