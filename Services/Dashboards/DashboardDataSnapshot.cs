using ONERI.Models;

namespace ONERI.Services.Dashboards;

public class DashboardDataSnapshot
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public List<SatirModeli> ProfilRows { get; set; } = new();
    public List<ProfilHataSatir> ProfilHataRows { get; set; } = new();

    public List<BoyaUretimSatir> BoyaUretimRows { get; set; } = new();
    public List<BoyaHataSatir> BoyaHataRows { get; set; } = new();

    public List<PvcSatirModel> PvcRows { get; set; } = new();
    public List<MasterwoodSatirModel> MasterwoodRows { get; set; } = new();
    public List<SkipperSatirModel> SkipperRows { get; set; } = new();
    public List<RoverBSatirModel> RoverBRows { get; set; } = new();
    public List<TezgahSatirModel> TezgahRows { get; set; } = new();
    public List<EbatlamaSatirModel> EbatlamaRows { get; set; } = new();

    public List<HataliParcaSatirModel> HataliParcaRows { get; set; } = new();
}
