using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class TezgahDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamParcaAdeti { get; set; }
        public double OrtalamaKisiSayisi { get; set; }
        public double ToplamKayipSureDakika { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> ParcaTrendData { get; set; } = new List<double>();
        public List<double> KisiTrendData { get; set; } = new List<double>();
        public List<double> KullanilabilirlikTrendData { get; set; } = new List<double>();

        public List<string> KayipNedenLabels { get; set; } = new List<string>();
        public List<double> KayipNedenData { get; set; } = new List<double>();
    }
}
