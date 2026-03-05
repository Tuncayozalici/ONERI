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
        Assert.Contains("Boya - Konveyör Hattı", result.Model.MakineOeeLabels);
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
