using ONERI.Models;

namespace ONERI.Services.Dashboards;

public interface IDashboardQueryService
{
    Task<DashboardPageResult<GenelFabrikaOzetViewModel>> GetGunlukVerilerAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<GunlukVerilerViewModel>> GetProfilLazerAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<BoyaDashboardViewModel>> GetBoyahaneAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<PvcDashboardViewModel>> GetPvcAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<CncDashboardViewModel>> GetCncAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<MasterwoodDashboardViewModel>> GetMasterwoodAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<SkipperDashboardViewModel>> GetSkipperAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<RoverBDashboardViewModel>> GetRoverBAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<TezgahDashboardViewModel>> GetTezgahAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<EbatlamaDashboardViewModel>> GetEbatlamaAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<HataliParcaDashboardViewModel>> GetHataliParcaAsync(DateTime? raporTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
}
