namespace Web.Core.DTOs
{
    public class NoReadAnalysisRecord
    {
        public string Scanner { get; set; } = string.Empty;
        public string? Plant { get; set; }
        public string? LineCode { get; set; }
        public int Hour { get; set; }
        public int NoReadCount { get; set; }
        public decimal NoReadPercent { get; set; }
    }
}
