using System;

namespace ONERI.Models
{
    public class PersonelYoklamaSatirModel
    {
        public DateTime Tarih { get; set; }
        public string? BolumAdi { get; set; }
        public int PersonelSayisi { get; set; }
        public string? Aciklama { get; set; }
    }
}
