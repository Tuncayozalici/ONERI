using System;

namespace ONERI.Models
{
    public class GunlukCalismaSatirModel
    {
        public DateTime Tarih { get; set; }
        public string? BolumAdi { get; set; }
        public double PlanUyumOrani { get; set; }
        public int ToplamModulSayisi { get; set; }
        public int DepoGirenModulSayisi { get; set; }
        public int ModulHedefi { get; set; }
        public bool ModulVerisiTahminiMi { get; set; }
    }
}
