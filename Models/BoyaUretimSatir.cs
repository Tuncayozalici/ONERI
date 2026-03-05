using System;

namespace ONERI.Models
{
    public class BoyaUretimSatir
    {
        public DateTime Tarih { get; set; }
        public int BoyananRenk { get; set; }
        public string? Makine { get; set; }
        public double HatHizi { get; set; }
        public int PanelAdet { get; set; }
        public bool OgleArasiCalisildi { get; set; }
        public string? Aciklama { get; set; }
        public int DosemeAdet { get; set; }
        public string? BuyukParcaCinsi { get; set; }
        public int BuyukParcaAdeti { get; set; }
        public string? KucukParcaCinsi { get; set; }
        public int KucukParcaAdeti { get; set; }
        public int KirliProfilParcaSayisi { get; set; }
        public int HataliParcaSayisi { get; set; }
        public string? DuraklamaNedeni1 { get; set; }
        public int DuraklamaSuresi1 { get; set; }
        public string? DuraklamaNedeni2 { get; set; }
        public int DuraklamaSuresi2 { get; set; }
        public string? DuraklamaNedeni3 { get; set; }
        public int DuraklamaSuresi3 { get; set; }
        public int Toplam { get; set; }
        public double PerformansIcinParcaSayisi { get; set; }
        public double Performans { get; set; }
        public double Kalite { get; set; }
        public double Kullanilabilirlik { get; set; }
        public double Oee { get; set; }

        public int DuraklamaDakikaToplam => DuraklamaSuresi1 + DuraklamaSuresi2 + DuraklamaSuresi3;

        public int ToplamBoyananParca => Toplam > 0
            ? Toplam
            : PanelAdet + DosemeAdet + BuyukParcaAdeti + KucukParcaAdeti + KirliProfilParcaSayisi;
    }
}
