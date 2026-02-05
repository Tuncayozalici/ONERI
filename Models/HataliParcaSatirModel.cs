using System;

namespace ONERI.Models
{
    public class HataliParcaSatirModel
    {
        public DateTime Tarih { get; set; }
        public string? BolumAdi { get; set; }
        public string? TalepAcanKullanici { get; set; }
        public string? SiparisNo { get; set; }
        public string? UrunIsmi { get; set; }
        public string? Renk { get; set; }
        public string? Kalinlik { get; set; }
        public double Boy { get; set; }
        public double En { get; set; }
        public double Adet { get; set; }
        public double ToplamM2 { get; set; }
        public string? HataNedeni { get; set; }
        public string? OperatorAdi { get; set; }
        public string? KesimDurumu { get; set; }
        public string? PvcDurumu { get; set; }
    }
}
