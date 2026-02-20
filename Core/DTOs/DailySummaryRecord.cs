using System;
namespace Web.Core.DTOs
{
    public class DailySummaryRecord
    {
        public DateTime ReportDate { get; set; }
        public int TotalBoxes { get; set; }
        public int Matched { get; set; }
        public int MissingAtTo { get; set; }
        public int MissingAtFrom { get; set; }
        public int BothFailed { get; set; }
        public decimal MatchRatePercent { get; set; }
        public int? AvgTransitSeconds { get; set; }
        public int FromNoReadCount { get; set; }
        public int ToNoReadCount { get; set; }
    }
}
