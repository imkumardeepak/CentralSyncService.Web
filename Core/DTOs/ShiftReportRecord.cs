using System;
namespace Web.Core.DTOs
{
    public class ShiftReportRecord
    {
        public string ShiftName { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public int Matched { get; set; }
        public int MissingAtTo { get; set; }
        public int MissingAtFrom { get; set; }
        public int BothFailed { get; set; }
        public decimal MatchRatePercent { get; set; }
        public int? AvgTransitSeconds { get; set; }
        public DateTime? ShiftStart { get; set; }
        public DateTime? ShiftEnd { get; set; }
    }
}
