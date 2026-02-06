using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONERI.Models
{
    public class Degerlendirme
    {
        public int Id { get; set; }
        public int OneriId { get; set; }

        public virtual Oneri? Oneri { get; set; }

        [Range(0, 25, ErrorMessage = "Gayret puanı 0-25 aralığında olmalıdır.")]
        public int GayretPuani { get; set; }

        [Range(0, 25, ErrorMessage = "Orijinallik puanı 0-25 aralığında olmalıdır.")]
        public int OrijinallikPuani { get; set; }

        [Range(0, 25, ErrorMessage = "Etki puanı 0-25 aralığında olmalıdır.")]
        public int EtkiPuani { get; set; }

        [Range(0, 25, ErrorMessage = "Uygulanabilirlik puanı 0-25 aralığında olmalıdır.")]
        public int UygulanabilirlikPuani { get; set; }

        [Range(0, 100, ErrorMessage = "Toplam puan 0-100 aralığında olmalıdır.")]
        public int ToplamPuan { get; set; }

        [MaxLength(1000)]
        public string KurulYorumu { get; set; } = "";
    }
}
