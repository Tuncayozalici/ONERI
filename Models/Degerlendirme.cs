using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ONERI.Models
{
    public class Degerlendirme
    {
        public int Id { get; set; }
        public int OneriId { get; set; }

        public virtual Oneri? Oneri { get; set; }

        public int GayretPuani { get; set; }
        public int OrijinallikPuani { get; set; }
        public int EtkiPuani { get; set; }
        public int UygulanabilirlikPuani { get; set; }
        public int ToplamPuan { get; set; }

        public string KurulYorumu { get; set; } = "";
    }
}