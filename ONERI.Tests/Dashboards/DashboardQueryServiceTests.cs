using ONERI.Models;
using ONERI.Services.Dashboards;
using Xunit;

namespace ONERI.Tests.Dashboards;

public class DashboardQueryServiceTests
{
    [Fact]
    public async Task GetGunlukVerilerAsync_SumsSummaryErrorFromAllThreeDashboards()
    {
        var selectedDate = new DateTime(2026, 2, 4);
        var snapshot = new DashboardDataSnapshot
        {
            ProfilHataRows = new List<ProfilHataSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "Metal",
                    HataNedeni = "Ürün Uyuşmazlığı",
                    Adet = 14
                }
            },
            BoyaHataRows = new List<BoyaHataSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    HataNedeni = "Boya Kusuru",
                    HataliAdet = 4
                }
            },
            HataliParcaRows = new List<HataliParcaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    HataNedeni = "Ürün Uyuşmazlığı",
                    Adet = 5
                },
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    HataNedeni = "Makine Hatası",
                    Adet = 99
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetGunlukVerilerAsync(
            raporTarihi: null,
            baslangicTarihi: selectedDate,
            bitisTarihi: selectedDate,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(41d, result.Model.ToplamHataAdet);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_IntegratesBoyaDuraklamaAndMachineOee()
    {
        var selectedDate = new DateTime(2026, 3, 4);
        var snapshot = new DashboardDataSnapshot
        {
            BoyaUretimRows = new List<BoyaUretimSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    Makine = "KONVEYÖR HATTI",
                    DuraklamaSuresi1 = 30,
                    DuraklamaSuresi2 = 10,
                    DuraklamaSuresi3 = 20,
                    Toplam = 900,
                    Oee = 42.5,
                    Performans = 50,
                    Kalite = 99,
                    Kullanilabilirlik = 80
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetGunlukVerilerAsync(
            raporTarihi: null,
            baslangicTarihi: selectedDate,
            bitisTarihi: selectedDate,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(60d, result.Model.ToplamDuraklamaDakika);
        Assert.Contains("Konveyör Hattı", result.Model.MakineOeeLabels);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_BuildsPlanUyumChartFromGunlukCalismaRows()
    {
        var selectedDate = new DateTime(2026, 4, 2);
        var snapshot = new DashboardDataSnapshot
        {
            GunlukCalismaRows = new List<GunlukCalismaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    PlanUyumOrani = 84
                },
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "Kesim",
                    PlanUyumOrani = 92
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetGunlukVerilerAsync(
            raporTarihi: null,
            baslangicTarihi: selectedDate,
            bitisTarihi: selectedDate,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { "Kesim", "PVC" }, result.Model.PlanUyumBolumLabels);
        Assert.Equal(new[] { 92d, 84d }, result.Model.PlanUyumBolumData);
    }

    [Fact]
    public async Task GetProfilLazerAsync_IgnoresNumericDowntimeReasonLabels()
    {
        var selectedDate = new DateTime(2026, 4, 15);
        var snapshot = new DashboardDataSnapshot
        {
            ProfilRows = new List<SatirModeli>
            {
                new()
                {
                    Tarih = selectedDate,
                    CalisilanMakine = "PROFİL LAZER MANUEL",
                    DuraklamaNedeni1 = "Personel Eksik",
                    DuraklamaSuresi1 = 25,
                    DuraklamaNedeni2 = "0",
                    DuraklamaSuresi2 = 25,
                    DuraklamaNedeni3 = "16",
                    DuraklamaSuresi3 = 260
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetProfilLazerAsync(
            raporTarihi: selectedDate,
            baslangicTarihi: null,
            bitisTarihi: null,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { "Personel Eksik" }, result.Model.DuraklamaNedenLabels);
        Assert.Equal(new[] { 25 }, result.Model.DuraklamaNedenData);
    }

    private sealed class StubDashboardIngestionService : IDashboardIngestionService
    {
        private readonly DashboardDataSnapshot _snapshot;

        public StubDashboardIngestionService(DashboardDataSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<DashboardDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshot);
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
