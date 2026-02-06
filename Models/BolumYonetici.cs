using System.ComponentModel.DataAnnotations;

namespace ONERI.Models
{
    public class BolumYonetici
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string BolumAdi { get; set; } = "";

        [Required]
        [MaxLength(120)]
        public string YoneticiAdi { get; set; } = "";

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string YoneticiEmail { get; set; } = "";
    }
}
