using System.Collections.Generic;

namespace ONERI.Models
{
    public class DuraklamaBolumDetayModel
    {
        public string Bolum { get; set; } = string.Empty;
        public double ToplamDuraklamaDakika { get; set; }
        public List<DuraklamaMakineDetayModel> MakineDetaylari { get; set; } = new List<DuraklamaMakineDetayModel>();
    }

    public class DuraklamaMakineDetayModel
    {
        public string Makine { get; set; } = string.Empty;
        public double DuraklamaDakika { get; set; }
    }
}
