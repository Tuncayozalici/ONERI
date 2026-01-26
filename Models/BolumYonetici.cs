using System.ComponentModel.DataAnnotations;

namespace ONERI.Models
{
    public class BolumYonetici
    {
        public int Id { get; set; }
        public string BolumAdi { get; set; } = "";
        public string YoneticiAdi { get; set; } = "";
        public string YoneticiEmail { get; set; } = "";
    }
}