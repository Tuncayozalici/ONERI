using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class SkipperDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamDelik { get; set; }
        public double OrtalamaKisiSayisi { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaFiiliCalismaOrani { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> DelikTrendData { get; set; } = new List<double>();
        public List<double> KisiTrendData { get; set; } = new List<double>();

        public List<double> UretimOraniTrendData { get; set; } = new List<double>();
        public List<double> KayipSureTrendData { get; set; } = new List<double>();
        public List<double> FiiliCalismaTrendData { get; set; } = new List<double>();

        public List<string> DuraklamaNedenLabels { get; set; } = new List<string>();
        public List<double> DuraklamaNedenData { get; set; } = new List<double>();
    }
}
