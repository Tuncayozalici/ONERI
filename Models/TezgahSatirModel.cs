using System;

namespace ONERI.Models
{
    public class TezgahSatirModel
    {
        public DateTime Tarih { get; set; }
        public string? TezgahUrunleri { get; set; }
        public double KisiSayisi { get; set; }
        public double ParcaAdeti { get; set; }
        public double SureDakika { get; set; }
        public string? CalismaKosulu { get; set; }
        public string? KayipSureNedeni { get; set; }
        public double KayipSureDakika { get; set; }
        public string? Aciklama { get; set; }
        public double Kullanilabilirlik { get; set; }
    }
}
