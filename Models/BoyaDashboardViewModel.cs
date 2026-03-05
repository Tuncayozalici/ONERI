using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class BoyaDashboardViewModel
    {
        public BoyaDashboardViewModel()
        {
            UretimTrendLabels = new List<string>();
            UretimTrendData = new List<double>();
            HedefTrendData = new List<double>();
            HataTrendData = new List<double>();
            PerformansTrendData = new List<double>();
            KaliteTrendData = new List<double>();
            KullanilabilirlikTrendData = new List<double>();
            OeeTrendData = new List<double>();
            MakineLabels = new List<string>();
            MakineUretimData = new List<double>();
            MakineOeeData = new List<double>();
            DuraklamaNedenLabels = new List<string>();
            DuraklamaNedenData = new List<double>();
            ParcaKarmaLabels = new List<string>();
            ParcaKarmaData = new List<double>();
            MakineKartlari = new List<MakineKartOzetModel>();
        }

        public DateTime RaporTarihi { get; set; }

        public double ToplamBoyananParca { get; set; }
        public double PanelBoyananParca { get; set; }
        public double DosemeBoyananParca { get; set; }
        public double BuyukParcaAdedi { get; set; }
        public double KucukParcaAdedi { get; set; }
        public double KirliProfilParca { get; set; }

        public double ToplamPerformansIcinParcaSayisi { get; set; }
        public double UretimHedefGerceklesmeOrani { get; set; }

        public double ToplamHataliParca { get; set; }
        public double HataOrani { get; set; }

        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaHatHizi { get; set; }

        public double OrtalamaPerformans { get; set; }
        public double OrtalamaKalite { get; set; }
        public double OrtalamaKullanilabilirlik { get; set; }
        public double OrtalamaOee { get; set; }

        public double ToplamKayitSayisi { get; set; }
        public double OgleArasiCalisilanKayitSayisi { get; set; }
        public double OgleArasiCalismaOrani { get; set; }

        public List<string> UretimTrendLabels { get; set; }
        public List<double> UretimTrendData { get; set; }
        public List<double> HedefTrendData { get; set; }
        public List<double> HataTrendData { get; set; }

        public List<double> PerformansTrendData { get; set; }
        public List<double> KaliteTrendData { get; set; }
        public List<double> KullanilabilirlikTrendData { get; set; }
        public List<double> OeeTrendData { get; set; }

        public List<string> MakineLabels { get; set; }
        public List<double> MakineUretimData { get; set; }
        public List<double> MakineOeeData { get; set; }

        public List<string> DuraklamaNedenLabels { get; set; }
        public List<double> DuraklamaNedenData { get; set; }

        public List<string> ParcaKarmaLabels { get; set; }
        public List<double> ParcaKarmaData { get; set; }

        public List<MakineKartOzetModel> MakineKartlari { get; set; }
    }
}
