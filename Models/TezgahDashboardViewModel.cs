using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class TezgahDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }
        public int CalisilanIsGunu { get; set; }
        public int OrtalamaCalisanPersonel { get; set; }

        public double ToplamParcaAdeti { get; set; }
        public double ToplamSureDakika { get; set; }
        public double ToplamNetSureDakika { get; set; }
        public double OrtalamaKisiSayisi { get; set; }
        public double ToplamKayipSureDakika { get; set; }
        public double ToplamHataliParcaSayisi { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaGunlukKapasite { get; set; }
        public double ToplamAdamSaat { get; set; }
        public double ToplamKalanAdamSaat { get; set; }
        public double OrtalamaVerimliCalismaOrani { get; set; }
        public double OrtalamaOee { get; set; }
        public double OrtalamaSaatlikUretim { get; set; }
        public int AktifUrunSayisi { get; set; }
        public int KayitSayisi { get; set; }
        public string? OneCikanUrun { get; set; }
        public string? BaskinKayipNedeni { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> GunlukParcaTrendData { get; set; } = new List<double>();
        public List<double> GunlukKayipSureTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();
        public List<double> PerformansTrendData { get; set; } = new List<double>();
        public List<double> KullanilabilirlikTrendData { get; set; } = new List<double>();
        public List<double> KaliteTrendData { get; set; } = new List<double>();

        public List<string> UrunLabels { get; set; } = new List<string>();
        public List<double> UrunParcaData { get; set; } = new List<double>();
        public List<double> UrunKayipSureData { get; set; } = new List<double>();

        public List<string> KayipNedenLabels { get; set; } = new List<string>();
        public List<double> KayipNedenData { get; set; } = new List<double>();

        public List<string> CalismaKosuluLabels { get; set; } = new List<string>();
        public List<double> CalismaKosuluKayipSureData { get; set; } = new List<double>();
        public List<double> CalismaKosuluParcaData { get; set; } = new List<double>();
    }
}
