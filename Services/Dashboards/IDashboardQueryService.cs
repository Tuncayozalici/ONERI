using ONERI.Models;

namespace ONERI.Services.Dashboards;

public interface IDashboardQueryService
{
    Task<DashboardPageResult<GenelFabrikaOzetViewModel>> GetGunlukVerilerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<GunlukVerilerViewModel>> GetProfilLazerAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<BoyaDashboardViewModel>> GetBoyahaneAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<PvcDashboardViewModel>> GetPvcAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<CncDashboardViewModel>> GetCncAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<MasterwoodDashboardViewModel>> GetMasterwoodAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<SkipperDashboardViewModel>> GetSkipperAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<RoverBDashboardViewModel>> GetRoverBAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<TezgahDashboardViewModel>> GetTezgahAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<EbatlamaDashboardViewModel>> GetEbatlamaAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, string? makine, CancellationToken cancellationToken = default);
    Task<DashboardPageResult<HataliParcaDashboardViewModel>> GetHataliParcaAsync(DateTime? raporTarihi, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? ay, int? yil, CancellationToken cancellationToken = default);
}
