using System.Collections.Generic;

namespace ONERI.Models
{
    public class KpiBolumDetayModel
    {
        public string Bolum { get; set; } = string.Empty;
        public double ToplamDeger { get; set; }
        public List<KpiMakineDetayModel> MakineDetaylari { get; set; } = new List<KpiMakineDetayModel>();
    }

    public class KpiMakineDetayModel
    {
        public string Makine { get; set; } = string.Empty;
        public double Deger { get; set; }
    }
}
