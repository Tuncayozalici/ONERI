namespace ONERI.Models;

public class ProfilLazerDashboardViewModel
{
    public DateTime RaporTarihi { get; set; } = DateTime.Today;
    public int CalisilanIsGunu { get; set; }
    public int OrtalamaCalisanPersonel { get; set; }

    public int ToplamKesilenProfilBoy { get; set; }
    public int ToplamParcaSayisi { get; set; }
    public int ToplamHataSayisi { get; set; }
    public int ToplamKullanilanSure { get; set; }
    public int ToplamKalanSure { get; set; }

    public double OrtalamaPerformans { get; set; }
    public double OrtalamaKullanilabilirlik { get; set; }
    public double OrtalamaKalite { get; set; }
    public double OrtalamaOee { get; set; }

    public List<string> TrendTarihleri { get; set; } = new();
    public List<double> OeeTrendData { get; set; } = new();
    public List<double> PerformansTrendData { get; set; } = new();
    public List<double> KullanilabilirlikTrendData { get; set; } = new();
    public List<double> KaliteTrendData { get; set; } = new();
    public List<int> HataTrendData { get; set; } = new();

    public List<string> MakineLabels { get; set; } = new();
    public List<double> MakineOeeData { get; set; } = new();
    public List<int> MakineKullanilanSureData { get; set; } = new();
    public List<int> MakineKalanSureData { get; set; } = new();

    public List<string> MusteriLabels { get; set; } = new();
    public List<int> MusteriParcaData { get; set; } = new();

    public List<string> ProfilLabels { get; set; } = new();
    public List<int> ProfilParcaData { get; set; } = new();

    public List<string> MesaiDurumuLabels { get; set; } = new();
    public List<int> MesaiDurumuData { get; set; } = new();

    public List<string> DuraklamaNedenLabels { get; set; } = new();
    public List<int> DuraklamaNedenData { get; set; } = new();
    public List<MakineDuraklamaNedenDagilimModel> MakineDuraklamaDagilimlari { get; set; } = new();
}
