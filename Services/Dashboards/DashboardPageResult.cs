namespace ONERI.Services.Dashboards;

public class DashboardPageResult<TModel>
{
    public required TModel Model { get; init; }
    public Dictionary<string, object?> ViewBagValues { get; init; } = new();
}
