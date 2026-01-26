using System.ComponentModel.DataAnnotations;

namespace ONERI.Models
{
    public class Oneri
    {
        public int Id { get; set; }

        [Display(Name = "Öneriyi Yapan")]
        public string? OnerenKisi { get; set; } // Soru işareti (?) boş kalabilir demek.

        [Required]
        [Display(Name = "Çalıştığı Bölüm")]
        public string CalistigiBolum { get; set; } = "";

        [Display(Name = "Alt Bölüm")]
        public string? AltBolum { get; set; } 

        [Required]
        public string Bolum { get; set; } = ""; // <--- = ""; EKLE

        [Required]
        public string Konu { get; set; } = ""; // <--- = ""; EKLE

        [Required]
        public string Aciklama { get; set; } = ""; // <--- = ""; EKLE

        public DateTime Tarih { get; set; } = DateTime.Now;
        
        public OneriDurum Durum { get; set; } = OneriDurum.Beklemede;

        // İlişki (Listeyi başlatıyoruz ki null olmasın)
        public virtual ICollection<Degerlendirme> Degerlendirmeler { get; set; } = new List<Degerlendirme>();
    }
}