using System.Collections.Generic;

namespace ONERI.Models
{
    public class GunlukVerilerViewModel
    {
        public GunlukVerilerViewModel()
        {
            ProfilIsimleri = new List<string>();
            ProfilUretimAdetleri = new List<int>();
            Son7GunTarihleri = new List<string>();
            GunlukUretimSayilari = new List<int>();
            UrunIsimleri = new List<string>();
            UrunHarcananSure = new List<int>();
            RaporTarihi = DateTime.Today;
        }

        // KPI Alanları
        public int GunlukToplamUretim { get; set; }
        public int GunlukToplamSure { get; set; }
        public double OrtalamaIslemSuresi { get; set; }

        // Pasta Grafik için
        public List<string> ProfilIsimleri { get; set; }
        public List<int> ProfilUretimAdetleri { get; set; }

        // Çizgi Grafik için
        public List<string> Son7GunTarihleri { get; set; }
        public List<int> GunlukUretimSayilari { get; set; }

        // Yeni Pasta Grafik için (Ürün bazlı süre dağılımı)
        public List<string> UrunIsimleri { get; set; }
        public List<int> UrunHarcananSure { get; set; }

        // Raporun hangi tarihe ait olduğunu tutmak için
        public DateTime RaporTarihi { get; set; }
    }
}
