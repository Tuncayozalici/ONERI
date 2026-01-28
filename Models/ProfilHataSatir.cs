using System;

namespace ONERI.Models
{
    public class ProfilHataSatir
    {
        public DateTime Tarih { get; set; }
        public string? BolumAdi { get; set; }
        public string? HataUrunSonucu { get; set; }
        public string? HataNedeni { get; set; }
        public int Adet { get; set; }
    }
}
