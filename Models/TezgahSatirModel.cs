using System;
using System.Collections.Generic;
using System.Linq;

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
        public string? KayipSureNedeni1 { get; set; }
        public double KayipSureDakika1 { get; set; }
        public string? KayipSureNedeni2 { get; set; }
        public double KayipSureDakika2 { get; set; }
        public string? KayipSureNedeni3 { get; set; }
        public double KayipSureDakika3 { get; set; }
        public string? Aciklama { get; set; }
        public double Kullanilabilirlik { get; set; }
        public double Oee { get; set; }

        public string? KayipSureNedeni => GetKayipSureKalemleri()
            .OrderByDescending(x => x.Dakika)
            .Select(x => x.Neden)
            .FirstOrDefault();

        public double KayipSureDakika => ToplamKayipSureDakika;

        public double ToplamKayipSureDakika => Math.Max(0, KayipSureDakika1)
            + Math.Max(0, KayipSureDakika2)
            + Math.Max(0, KayipSureDakika3);

        public double NetCalismaSureDakika => Math.Max(0, SureDakika - ToplamKayipSureDakika);

        public IEnumerable<(string Neden, double Dakika)> GetKayipSureKalemleri()
        {
            if (!string.IsNullOrWhiteSpace(KayipSureNedeni1) && KayipSureDakika1 > 0)
            {
                yield return (KayipSureNedeni1.Trim(), KayipSureDakika1);
            }

            if (!string.IsNullOrWhiteSpace(KayipSureNedeni2) && KayipSureDakika2 > 0)
            {
                yield return (KayipSureNedeni2.Trim(), KayipSureDakika2);
            }

            if (!string.IsNullOrWhiteSpace(KayipSureNedeni3) && KayipSureDakika3 > 0)
            {
                yield return (KayipSureNedeni3.Trim(), KayipSureDakika3);
            }
        }
    }
}
