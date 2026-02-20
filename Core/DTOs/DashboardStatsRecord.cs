namespace Web.Core.DTOs
{
    public class DashboardStatsRecord
    {
        public string Period { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public int Matched { get; set; }
        public int Issues { get; set; }
        public int Pending { get; set; }
        public int? AvgTransitSec { get; set; }
    }
}
