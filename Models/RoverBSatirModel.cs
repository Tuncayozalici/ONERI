using System;

namespace ONERI.Models
{
    public class RoverBSatirModel
    {
        public DateTime Tarih { get; set; }
        public double KisiSayisi { get; set; }
        public double DelikFreezeSayisi { get; set; }
        public double DelikFreezePvcSayisi { get; set; }
        public string? CalismaKosulu { get; set; }
        public double Duraklama1 { get; set; }
        public string? DuraklamaNedeni1 { get; set; }
        public double Duraklama2 { get; set; }
        public string? DuraklamaNedeni2 { get; set; }
        public double Duraklama3 { get; set; }
        public string? DuraklamaNedeni3 { get; set; }
        public double Duraklama4 { get; set; }
        public string? DuraklamaNedeni4 { get; set; }
        public double HataliParca { get; set; }
        public string? Aciklama { get; set; }
        public double Performans { get; set; }
        public double UretimOrani { get; set; }
        public double KayipSureOrani { get; set; }
        public double Kullanilabilirlik { get; set; }
        public double Kalite { get; set; }
        public double Oee { get; set; }
        public double FiiliCalismaOrani { get; set; }
    }
}
