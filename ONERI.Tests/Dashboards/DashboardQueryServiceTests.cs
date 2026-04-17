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

        Assert.Equal(136d, result.Model.ToplamHataAdet);
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
    public async Task GetGunlukVerilerAsync_UsesDepotModuleInsteadOfMontajModule()
    {
        var selectedDate = new DateTime(2026, 4, 10);
        var snapshot = new DashboardDataSnapshot
        {
            GunlukCalismaRows = new List<GunlukCalismaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "Montaj",
                    ToplamModulSayisi = 999,
                    DepoGirenModulSayisi = 0
                },
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    ToplamModulSayisi = 120,
                    DepoGirenModulSayisi = 86,
                    ModulHedefi = 85
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

        Assert.Equal(86, result.Model.ToplamModulSayisi);
        Assert.False(result.Model.ModulSayisiTahminiMi);
        var pvc = Assert.Single(result.Model.BolumHedefKartlari, x => x.Bolum == "PVC");
        Assert.Equal("over-target", pvc.Durum);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_EstimatesDepotModuleWhenSourceIsMissing()
    {
        var selectedDate = new DateTime(2026, 4, 11);
        var snapshot = new DashboardDataSnapshot
        {
            GunlukCalismaRows = new List<GunlukCalismaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "CNC",
                    ToplamModulSayisi = 100,
                    DepoGirenModulSayisi = 0
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

        Assert.True(result.Model.ToplamModulSayisi > 0);
        Assert.True(result.Model.ToplamModulSayisi < 100);
        Assert.True(result.Model.ModulSayisiTahminiMi);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_ClassifiesTargetStatusWithThresholds()
    {
        var selectedDate = new DateTime(2026, 4, 12);
        var snapshot = new DashboardDataSnapshot
        {
            GunlukCalismaRows = new List<GunlukCalismaSatirModel>
            {
                new() { Tarih = selectedDate, BolumAdi = "Metal", DepoGirenModulSayisi = 57, ModulHedefi = 60 },
                new() { Tarih = selectedDate, BolumAdi = "PVC", DepoGirenModulSayisi = 84, ModulHedefi = 85 },
                new() { Tarih = selectedDate, BolumAdi = "CNC", DepoGirenModulSayisi = 100, ModulHedefi = 90 }
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

        Assert.Equal("on-target", result.Model.BolumHedefKartlari.Single(x => x.Bolum == "Metal").Durum);
        Assert.Equal("on-target", result.Model.BolumHedefKartlari.Single(x => x.Bolum == "PVC").Durum);
        Assert.Equal("over-target", result.Model.BolumHedefKartlari.Single(x => x.Bolum == "CNC").Durum);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_UsesDirectAndIndirectPersonnelInAverage()
    {
        var selectedDate = new DateTime(2026, 4, 13);
        var snapshot = new DashboardDataSnapshot
        {
            PersonelRows = new List<PersonelYoklamaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    PersonelSayisi = 10,
                    DirektPersonelSayisi = 10,
                    EndirektPersonelSayisi = 3,
                    ToplamPersonelSayisi = 13
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

        Assert.Equal(13, result.Model.OrtalamaCalisanPersonel);
        Assert.False(result.Model.PersonelTahminiMi);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_ClassifiesPersonnelByFactoryWorkType()
    {
        var selectedDate = new DateTime(2026, 4, 13);
        var snapshot = new DashboardDataSnapshot
        {
            PersonelRows = new List<PersonelYoklamaSatirModel>
            {
                new() { Tarih = selectedDate, BolumAdi = "Makine Operatörü", PersonelSayisi = 2 },
                new() { Tarih = selectedDate, BolumAdi = "Montaj", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Kaynak", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Paketleme", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Üretim Hattı Ustabaşı", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Bakım", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Kalite Kontrol", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Planlama", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Depo / Lojistik", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Mühendisler", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Yönetim / Ofis", PersonelSayisi = 1 },
                new() { Tarih = selectedDate, BolumAdi = "Temizlik / Güvenlik", PersonelSayisi = 1 }
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

        Assert.Equal(new[] { "Direkt Çalışan Sayısı", "Endirekt Çalışan Sayısı" }, result.Model.PersonelOzetLabels);
        Assert.Equal(6d, result.Model.PersonelOzetData[0]);
        Assert.Equal(7d, result.Model.PersonelOzetData[1]);
        Assert.Equal(13, result.Model.OrtalamaCalisanPersonel);
        Assert.False(result.Model.PersonelTahminiMi);
    }

    [Fact]
    public async Task GetGunlukVerilerAsync_UsesOnlyBoyaHataRowsForBoyahaneErrors()
    {
        var selectedDate = new DateTime(2026, 4, 15);
        var snapshot = new DashboardDataSnapshot
        {
            HataliParcaRows = new List<HataliParcaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = null,
                    HataNedeni = null,
                    OperatorAdi = null,
                    Adet = 0
                }
            },
            BoyaUretimRows = new List<BoyaUretimSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    Makine = "KONVEYÖR HATTI",
                    HataliParcaSayisi = 500
                }
            },
            BoyaHataRows = new List<BoyaHataSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    HataNedeni = "Boya Kusuru",
                    HataliAdet = 7
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

        Assert.Equal(7d, result.Model.ToplamHataAdet);

        var boyahaneDetay = Assert.Single(result.Model.HataBolumDetaylari);
        Assert.Equal("Boyahane", boyahaneDetay.Bolum);
        Assert.Equal(7d, boyahaneDetay.ToplamDeger);

        var makineDetay = Assert.Single(boyahaneDetay.MakineDetaylari);
        Assert.Equal("Makine bilgisi yok", makineDetay.Makine);
        Assert.Equal(7d, makineDetay.Deger);
    }

    [Fact]
    public async Task GetHataliParcaAsync_UsesOnlyBoyaHataRowsForBoyahaneCharts()
    {
        var selectedDate = new DateTime(2026, 4, 15);
        var snapshot = new DashboardDataSnapshot
        {
            BoyaUretimRows = new List<BoyaUretimSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    Makine = "KONVEYÖR HATTI",
                    Aciklama = "Üretim Excel Hatası",
                    HataliParcaSayisi = 500
                }
            },
            BoyaHataRows = new List<BoyaHataSatir>
            {
                new()
                {
                    Tarih = selectedDate,
                    HataNedeni = "Boya Kusuru",
                    HataliAdet = 7
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetHataliParcaAsync(
            raporTarihi: selectedDate,
            baslangicTarihi: null,
            bitisTarihi: null,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(7d, result.Model.ToplamHataAdet);
        Assert.Equal(new[] { "Boyahane" }, result.Model.BolumLabels);
        Assert.Equal(new[] { 100d }, result.Model.BolumData);
        Assert.Equal(new[] { 7d }, result.Model.BolumAdetData);
        Assert.Equal(new[] { "Yüzey / kaplama" }, result.Model.HataNedenLabels);
        Assert.Equal(new[] { 100d }, result.Model.HataNedenData);
        Assert.Equal(new[] { 7d }, result.Model.HataNedenAdetData);
        Assert.DoesNotContain("Bilinmeyen", result.Model.BolumLabels);
        Assert.DoesNotContain("Bilinmeyen", result.Model.HataNedenLabels);
        Assert.DoesNotContain(500d, result.Model.HataAdetTrendData);
    }

    [Fact]
    public async Task GetHataliParcaAsync_NormalizesMachineAndPersonnelErrors()
    {
        var selectedDate = new DateTime(2026, 4, 16);
        var snapshot = new DashboardDataSnapshot
        {
            PvcRows = new List<PvcSatirModel>
            {
                new() { Tarih = selectedDate, ParcaSayisi = 200 }
            },
            HataliParcaRows = new List<HataliParcaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    HataNedeni = "Makine Hatası",
                    UrunIsmi = "Polo Masa",
                    Adet = 4
                },
                new()
                {
                    Tarih = selectedDate,
                    BolumAdi = "PVC",
                    HataNedeni = "Personel Hatası",
                    UrunIsmi = "Polo Masa",
                    Adet = 6
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetHataliParcaAsync(
            raporTarihi: selectedDate,
            baslangicTarihi: null,
            bitisTarihi: null,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(10d, result.Model.ToplamHataAdet);
        Assert.Equal(200d, result.Model.ToplamUretimAdet);
        Assert.Equal(5d, result.Model.HataliParcaOrani);
        Assert.DoesNotContain("Makine Hatası", result.Model.HataNedenLabels);
        Assert.DoesNotContain("Personel Hatası", result.Model.HataNedenLabels);
        Assert.Contains("Proses ayarı / ekipman parametresi", result.Model.HataNedenLabels);
        Assert.Contains("Operasyon / uygulama", result.Model.HataNedenLabels);
    }

    [Fact]
    public async Task GetHataliParcaAsync_BuildsThreeSixTwelveMonthReasonAnalysis()
    {
        var selectedDate = new DateTime(2026, 4, 16);
        var snapshot = new DashboardDataSnapshot
        {
            HataliParcaRows = new List<HataliParcaSatirModel>
            {
                new() { Tarih = selectedDate.AddMonths(-1), BolumAdi = "CNC", HataNedeni = "Delik Hatası", UrunIsmi = "A", Adet = 4 },
                new() { Tarih = selectedDate.AddMonths(-4), BolumAdi = "PVC", HataNedeni = "PVC Hatası", UrunIsmi = "B", Adet = 6 },
                new() { Tarih = selectedDate.AddMonths(-10), BolumAdi = "Kesim", HataNedeni = "Yüzey Çizik", UrunIsmi = "C", Adet = 8 }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetHataliParcaAsync(
            raporTarihi: selectedDate,
            baslangicTarihi: selectedDate.AddMonths(-12),
            bitisTarihi: selectedDate,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { 3, 6, 12 }, result.Model.HataDonemAnalizleri.Select(x => x.AySayisi));
        Assert.Contains("Delik / CNC işlemi", result.Model.HataDonemAnalizleri.Single(x => x.AySayisi == 3).NedenLabels);
        Assert.Contains("PVC / bantlama", result.Model.HataDonemAnalizleri.Single(x => x.AySayisi == 6).NedenLabels);
        Assert.Contains("Yüzey / kaplama", result.Model.HataDonemAnalizleri.Single(x => x.AySayisi == 12).NedenLabels);
    }

    [Fact]
    public async Task GetHataliParcaAsync_BuildsModuleBasedErrorAnalysis()
    {
        var selectedDate = new DateTime(2026, 4, 16);
        var snapshot = new DashboardDataSnapshot
        {
            HataliParcaRows = new List<HataliParcaSatirModel>
            {
                new() { Tarih = selectedDate, BolumAdi = "PVC", HataNedeni = "PVC Hatası", UrunIsmi = "Layer Dolap", Adet = 7 },
                new() { Tarih = selectedDate, BolumAdi = "PVC", HataNedeni = "PVC Hatası", UrunIsmi = "Layer Dolap", Adet = 3 },
                new() { Tarih = selectedDate, BolumAdi = "CNC", HataNedeni = "Delik Hatası", SiparisNo = "S-1", Adet = 2 }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetHataliParcaAsync(
            raporTarihi: selectedDate,
            baslangicTarihi: null,
            bitisTarihi: null,
            ay: null,
            yil: null,
            cancellationToken: CancellationToken.None);

        var module = result.Model.ModulHataAnalizleri.First();
        Assert.Equal("Layer Dolap", module.Modul);
        Assert.Equal(10d, module.HataAdet);
        Assert.True(result.Model.ModulAnaliziTahminiMi);
    }

    [Fact]
    public async Task GetEbatlamaAsync_UsesFirstReasonWhenOnlySecondDowntimeDurationIsFilled()
    {
        var selectedDate = new DateTime(2026, 4, 15);
        var snapshot = new DashboardDataSnapshot
        {
            EbatlamaRows = new List<EbatlamaSatirModel>
            {
                new()
                {
                    Tarih = selectedDate,
                    Makine = "SELCO",
                    Duraklama1 = 0,
                    DuraklamaNedeni1 = "İŞ BEKLEME",
                    Duraklama2 = 540,
                    DuraklamaNedeni2 = null
                }
            }
        };

        var service = new DashboardQueryService(new StubDashboardIngestionService(snapshot));

        var result = await service.GetEbatlamaAsync(
            raporTarihi: null,
            baslangicTarihi: selectedDate,
            bitisTarihi: selectedDate,
            ay: null,
            yil: null,
            makine: "SELCO",
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { "İŞ BEKLEME" }, result.Model.DuraklamaNedenLabels);
        Assert.Equal(new[] { 540d }, result.Model.DuraklamaNedenData);
        Assert.DoesNotContain("Bilinmiyor", result.Model.DuraklamaNedenLabels);
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
