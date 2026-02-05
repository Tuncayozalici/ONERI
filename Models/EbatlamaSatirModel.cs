using System;

namespace ONERI.Models
{
    public class EbatlamaSatirModel
    {
        public DateTime Tarih { get; set; }
        public string? Makine { get; set; }
        public double Plaka8Mm { get; set; }
        public double Kesim8MmAdet { get; set; }
        public double Plaka18Mm { get; set; }
        public double Plaka30Mm { get; set; }
        public double Kesim30MmAdet { get; set; }
        public double ToplamKesimAdet { get; set; }
        public double Gonyelleme { get; set; }
        public string? MesaiDurumu { get; set; }
        public double HazirlikMalzemeDakika { get; set; }
        public double Duraklama1 { get; set; }
        public string? DuraklamaNedeni1 { get; set; }
        public double Duraklama2 { get; set; }
        public string? DuraklamaNedeni2 { get; set; }
    }
}
