namespace Web.Core.DTOs
{
    public class BoxTrackingSummary
    {
        public int TotalBoxes { get; set; }
        public int Matched { get; set; }
        public int MissingAtTo { get; set; }
        public int MissingAtFrom { get; set; }
        public int BothFailed { get; set; }
        public int PendingTo { get; set; }
        public int PendingFrom { get; set; }
        public decimal MatchRatePercent { get; set; }
        public int? AvgTransitSeconds { get; set; }
    }
}
