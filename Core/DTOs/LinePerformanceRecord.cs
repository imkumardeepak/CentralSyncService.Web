using System;
namespace Web.Core.DTOs
{
    public class LinePerformanceRecord
    {
        public string LineCode { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public int Matched { get; set; }
        public int Issues { get; set; }
        public decimal MatchRatePercent { get; set; }
        public int? AvgTransitSeconds { get; set; }
        public DateTime? FirstScan { get; set; }
        public DateTime? LastScan { get; set; }
    }
}
