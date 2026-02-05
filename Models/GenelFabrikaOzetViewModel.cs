using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class GenelFabrikaOzetViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamUretim { get; set; }
        public double ToplamHataAdet { get; set; }
        public double ToplamHataM2 { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaFiiliCalisma { get; set; }

        public string EnCokHataNedeni { get; set; } = "-";
        public string EnCokHataBolum { get; set; } = "-";
        public string EnCokHataOperator { get; set; } = "-";

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> UretimTrendData { get; set; } = new List<double>();
        public List<double> HataTrendData { get; set; } = new List<double>();
        public List<double> DuraklamaTrendData { get; set; } = new List<double>();

        public List<string> BolumUretimLabels { get; set; } = new List<string>();
        public List<double> BolumUretimData { get; set; } = new List<double>();

        public List<string> HataNedenLabels { get; set; } = new List<string>();
        public List<double> HataNedenData { get; set; } = new List<double>();
    }
}
