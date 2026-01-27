using System.Collections.Generic;

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
}
