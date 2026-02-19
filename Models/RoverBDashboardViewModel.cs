using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class RoverBDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamDelikFreeze { get; set; }
        public double ToplamDelikFreezePvc { get; set; }
        public double ToplamHataliParca { get; set; }
        public double OrtalamaKisiSayisi { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaOee { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> DelikFreezeTrendData { get; set; } = new List<double>();
        public List<double> DelikFreezePvcTrendData { get; set; } = new List<double>();
        public List<double> HataliParcaTrendData { get; set; } = new List<double>();

        public List<string> DuraklamaNedenLabels { get; set; } = new List<string>();
        public List<double> DuraklamaNedenData { get; set; } = new List<double>();

        public List<double> UretimOraniTrendData { get; set; } = new List<double>();
        public List<double> KayipSureTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();
    }
}
