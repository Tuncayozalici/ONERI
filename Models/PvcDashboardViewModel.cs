using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class PvcDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamUretimMetraj { get; set; }
        public double ToplamParcaSayisi { get; set; }
        public double OrtalamaFiiliCalismaOrani { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaOee { get; set; }

        public List<string> UretimTrendLabels { get; set; } = new List<string>();
        public List<double> UretimTrendData { get; set; } = new List<double>();

        public List<string> MakineLabels { get; set; } = new List<string>();
        public List<double> MakineUretimData { get; set; } = new List<double>();
        public List<double> MakineParcaData { get; set; } = new List<double>();

        public List<string> DuraklamaNedenLabels { get; set; } = new List<string>();
        public List<double> DuraklamaNedenData { get; set; } = new List<double>();

        public List<string> FiiliCalismaLabels { get; set; } = new List<string>();
        public List<double> FiiliCalismaData { get; set; } = new List<double>();
        public List<double> KayipSureData { get; set; } = new List<double>();
        public List<double> UretimOraniTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();
        public List<string> MakineOeeSerieLabels { get; set; } = new List<string>();
        public List<List<double>> MakineOeeTrendSeries { get; set; } = new List<List<double>>();
    }
}
