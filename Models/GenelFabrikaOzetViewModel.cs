using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class GenelFabrikaOzetViewModel
    {
        public DateTime RaporTarihi { get; set; }
        public int CalisilanIsGunu { get; set; }
        public int OrtalamaCalisanPersonel { get; set; }
        public int ToplamModulSayisi { get; set; }

        public double ToplamUretim { get; set; }
        public double ToplamHataAdet { get; set; }
        public double ToplamHataM2 { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaOee { get; set; }
        public double OrtalamaFiiliCalisma { get; set; }

        public string EnCokHataNedeni { get; set; } = "-";
        public string EnCokHataBolum { get; set; } = "-";
        public string EnCokHataOperator { get; set; } = "-";

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> ModulTrendData { get; set; } = new List<double>();
        public List<double> UretimTrendData { get; set; } = new List<double>();
        public List<double> HataTrendData { get; set; } = new List<double>();
        public List<IstasyonDolulukSeriModel> IstasyonDolulukSerileri { get; set; } = new List<IstasyonDolulukSeriModel>();
        public List<double> DuraklamaTrendData { get; set; } = new List<double>();
        public List<KpiBolumDetayModel> UretimBolumDetaylari { get; set; } = new List<KpiBolumDetayModel>();
        public List<KpiBolumDetayModel> HataBolumDetaylari { get; set; } = new List<KpiBolumDetayModel>();
        public List<DuraklamaBolumDetayModel> DuraklamaBolumDetaylari { get; set; } = new List<DuraklamaBolumDetayModel>();

        public List<string> BolumUretimLabels { get; set; } = new List<string>();
        public List<double> BolumUretimData { get; set; } = new List<double>();
        public List<string> BolumHataLabels { get; set; } = new List<string>();
        public List<double> BolumHataData { get; set; } = new List<double>();
        public List<string> PersonelBolumLabels { get; set; } = new List<string>();
        public List<double> PersonelBolumData { get; set; } = new List<double>();
        public List<string> PlanUyumBolumLabels { get; set; } = new List<string>();
        public List<double> PlanUyumBolumData { get; set; } = new List<double>();

        public List<string> HataNedenLabels { get; set; } = new List<string>();
        public List<double> HataNedenData { get; set; } = new List<double>();

        public List<string> MakineOeeLabels { get; set; } = new List<string>();
        public List<double> MakineOeeData { get; set; } = new List<double>();

        public List<string> BolumOeeLabels { get; set; } = new List<string>();
        public List<double> BolumOeeData { get; set; } = new List<double>();
    }
}
