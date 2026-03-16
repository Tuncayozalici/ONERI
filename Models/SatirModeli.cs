using System;

namespace ONERI.Models
{
    public class SatirModeli
    {
        public DateTime Tarih { get; set; }
        public string? MusteriAdi { get; set; }
        public string? CalisilanMakine { get; set; }
        public string? MesaiDurumu { get; set; }
        public string? ProfilTipi { get; set; }
        public int UretimAdedi { get; set; }
        public int CalismaSuresi { get; set; }
        public string? DuraklamaNedeni1 { get; set; }
        public int DuraklamaSuresi1 { get; set; }
        public string? DuraklamaNedeni2 { get; set; }
        public int DuraklamaSuresi2 { get; set; }
        public string? DuraklamaNedeni3 { get; set; }
        public int DuraklamaSuresi3 { get; set; }
        public string? Aciklama { get; set; }
    }
}
