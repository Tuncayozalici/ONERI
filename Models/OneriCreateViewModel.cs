using System.ComponentModel.DataAnnotations;

namespace ONERI.Models
{
    public class OneriCreateViewModel
    {
        [Display(Name = "Adınız Soyadınız (İsteğe Bağlı)")]
        public string? OnerenKisi { get; set; }

        [Required(ErrorMessage = "Lütfen çalıştığınız bölümü belirtin.")]
        [Display(Name = "Çalıştığınız Bölüm")]
        public string CalistigiBolum { get; set; } = "";

        [Display(Name = "Alt Bölüm (Varsa)")]
        public string? AltBolum { get; set; } 

        [Required(ErrorMessage = "Lütfen önerinizin ilgili olduğu bölümü seçin.")]
        [Display(Name = "Önerinin İlgili Olduğu Bölüm")]
        public string Bolum { get; set; } = "";

        [Required(ErrorMessage = "Lütfen önerinize bir konu başlığı yazın.")]
        [MaxLength(100)]
        [Display(Name = "Konu")]
        public string Konu { get; set; } = "";

        [Required(ErrorMessage = "Lütfen önerinizi detaylı olarak açıklayın.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Açıklama")]
        public string Aciklama { get; set; } = "";
    }
}
