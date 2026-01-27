namespace ONERI.Models
{
    public class ChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
    }

    public class StackedBarChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> PanelData { get; set; } = new List<int>();
        public List<int> DosemeData { get; set; } = new List<int>();
    }

    public class DashboardViewModel
    {
        public int GunlukToplamBoyama { get; set; }
        public int PanelToplamBoyama { get; set; }
        public int DosemeToplamBoyama { get; set; }
        public int GunlukHataSayisi { get; set; }
        public double FireHataOrani { get; set; }

        public StackedBarChartData UretimDagilimi { get; set; } = new StackedBarChartData();
        public ChartData HataNedenleri { get; set; } = new ChartData();
        public ChartData KaliteTrendi { get; set; } = new ChartData();
        public ChartData UretimTrendi { get; set; } = new ChartData();
    }
}
