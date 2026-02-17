using System;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class CncDashboardViewModel
    {
        public DateTime RaporTarihi { get; set; }

        public double ToplamUretim { get; set; }
        public double ToplamDuraklamaDakika { get; set; }
        public double OrtalamaPerformans { get; set; }
        public double OrtalamaOee { get; set; }

        public List<string> TrendLabels { get; set; } = new List<string>();
        public List<double> UretimTrendData { get; set; } = new List<double>();
        public List<double> OeeTrendData { get; set; } = new List<double>();

        public CncMachineSummary Masterwood { get; set; } = new CncMachineSummary();
        public CncMachineSummary Skipper { get; set; } = new CncMachineSummary();
        public CncMachineSummary RoverB { get; set; } = new CncMachineSummary();
    }

    public class CncMachineSummary
    {
        public double Uretim { get; set; }
        public double DuraklamaDakika { get; set; }
        public double Oee { get; set; }
    }
}
