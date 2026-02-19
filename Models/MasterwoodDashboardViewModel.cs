using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class MasterwoodDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamDelik { get; set; }
        public double ToplamDelikFreeze { get; set; }
        public double ToplamHataliParca { get; set; }
        public double OrtalamaKisiSayisi { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaOee { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> DelikTrendData { get; set; } = new List<double>();
        public List<double> DelikFreezeTrendData { get; set; } = new List<double>();
        public List<double> HataliParcaTrendData { get; set; } = new List<double>();
        public List<double> KisiTrendData { get; set; } = new List<double>();

        public List<string> CalismaKosuluLabels { get; set; } = new List<string>();
        public List<double> CalismaKosuluData { get; set; } = new List<double>();

        public List<string> DuraklamaNedenLabels { get; set; } = new List<string>();
        public List<double> DuraklamaNedenData { get; set; } = new List<double>();

        public List<double> UretimOraniTrendData { get; set; } = new List<double>();
        public List<double> KayipSureTrendData { get; set; } = new List<double>();
        public List<double> FiiliCalismaTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();
    }
}
