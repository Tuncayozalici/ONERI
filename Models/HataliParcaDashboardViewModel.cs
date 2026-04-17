using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class HataliParcaDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }
        public int CalisilanIsGunu { get; set; }
        public int OrtalamaCalisanPersonel { get; set; }

        public double ToplamHataAdet { get; set; }
        public double ToplamHataM2 { get; set; }
        public double ToplamUretimAdet { get; set; }
        public double HataliParcaOrani { get; set; }
        public string EnCokHataNedeni { get; set; } = "-";
        public string EnCokHataBolum { get; set; } = "-";
        public string EnCokOperator { get; set; } = "-";

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> HataAdetTrendData { get; set; } = new List<double>();
        public List<double> HataM2TrendData { get; set; } = new List<double>();
        public List<double> HataliParcaOraniTrendData { get; set; } = new List<double>();

        public List<string> HataNedenLabels { get; set; } = new List<string>();
        public List<double> HataNedenData { get; set; } = new List<double>();
        public List<double> HataNedenAdetData { get; set; } = new List<double>();
        public List<BolumBazliHataNedenViewModel> BolumBazliHataNedenleri { get; set; } = new List<BolumBazliHataNedenViewModel>();

        public List<string> BolumLabels { get; set; } = new List<string>();
        public List<double> BolumData { get; set; } = new List<double>();
        public List<double> BolumAdetData { get; set; } = new List<double>();

        public List<string> OperatorLabels { get; set; } = new List<string>();
        public List<double> OperatorData { get; set; } = new List<double>();

        public List<string> KalinlikLabels { get; set; } = new List<string>();
        public List<double> KalinlikData { get; set; } = new List<double>();

        public List<string> RenkLabels { get; set; } = new List<string>();
        public List<double> RenkData { get; set; } = new List<double>();

        public List<string> KesimDurumLabels { get; set; } = new List<string>();
        public List<double> KesimDurumData { get; set; } = new List<double>();

        public List<string> PvcDurumLabels { get; set; } = new List<string>();
        public List<double> PvcDurumData { get; set; } = new List<double>();
        public List<HataDonemAnaliziViewModel> HataDonemAnalizleri { get; set; } = new List<HataDonemAnaliziViewModel>();
        public List<ModulHataAnaliziViewModel> ModulHataAnalizleri { get; set; } = new List<ModulHataAnaliziViewModel>();
        public bool ModulAnaliziTahminiMi { get; set; }
    }

    public class BolumBazliHataNedenViewModel
    {
        public string Bolum { get; set; } = "Bilinmeyen";
        public List<string> NedenLabels { get; set; } = new List<string>();
        public List<double> NedenData { get; set; } = new List<double>();
        public List<double> NedenAdetData { get; set; } = new List<double>();
    }

    public class HataDonemAnaliziViewModel
    {
        public string Baslik { get; set; } = string.Empty;
        public int AySayisi { get; set; }
        public List<string> NedenLabels { get; set; } = new List<string>();
        public List<double> NedenPayData { get; set; } = new List<double>();
        public List<double> NedenAdetData { get; set; } = new List<double>();
    }

    public class ModulHataAnaliziViewModel
    {
        public string Modul { get; set; } = string.Empty;
        public string Bolum { get; set; } = string.Empty;
        public double HataAdet { get; set; }
        public double HataPayi { get; set; }
        public bool TahminiMi { get; set; }
    }
}
