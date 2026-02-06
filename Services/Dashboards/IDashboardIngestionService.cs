namespace ONERI.Services.Dashboards;

public interface IDashboardIngestionService
{
    Task<DashboardDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
