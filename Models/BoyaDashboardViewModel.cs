
using System.Collections.Generic;

namespace ONERI.Models
{
    public class BoyaDashboardViewModel
    {
        public BoyaDashboardViewModel()
        {
            HataNedenleriListesi = new List<string>();
            HataSayilariListesi = new List<int>();
        }

        public double GunlukToplamBoyama { get; set; }
        public double PanelToplamBoyama { get; set; }
        public double DosemeToplamBoyama { get; set; }
        public double GunlukHataSayisi { get; set; }
        public double FireOrani { get; set; }
        public List<string> HataNedenleriListesi { get; set; }
        public List<int> HataSayilariListesi { get; set; }
        
        public StackedBarChartData UretimDagilimi { get; set; } = new StackedBarChartData();
        public ChartData KaliteTrendi { get; set; } = new ChartData();
        public ChartData UretimTrendi { get; set; } = new ChartData();
    }
}
