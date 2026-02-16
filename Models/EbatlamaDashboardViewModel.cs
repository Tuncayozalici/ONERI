using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class EbatlamaDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamKesimAdet { get; set; }
        public double ToplamPlaka8Mm { get; set; }
        public double ToplamPlaka18Mm { get; set; }
        public double ToplamPlaka30Mm { get; set; }
        public double ToplamGonyelleme { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaOee { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> KesimTrendData { get; set; } = new List<double>();
        public List<double> Plaka8TrendData { get; set; } = new List<double>();
        public List<double> Plaka18TrendData { get; set; } = new List<double>();
        public List<double> Plaka30TrendData { get; set; } = new List<double>();
        public List<double> Kesim8TrendData { get; set; } = new List<double>();
        public List<double> Kesim30TrendData { get; set; } = new List<double>();
        public List<double> GonyellemeTrendData { get; set; } = new List<double>();
        public List<double> HazirlikTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();

        public List<string> MakineLabels { get; set; } = new List<string>();
        public List<double> MakineKesimData { get; set; } = new List<double>();

        public List<string> MesaiLabels { get; set; } = new List<string>();
        public List<double> MesaiData { get; set; } = new List<double>();

        public List<string> DuraklamaNedenLabels { get; set; } = new List<string>();
        public List<double> DuraklamaNedenData { get; set; } = new List<double>();
    }
}
