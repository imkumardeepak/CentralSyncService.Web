namespace Web.Core.DTOs
{
    public class NoReadStatisticsRecord
    {
        public int HourNum { get; set; }
        public string HourLabel { get; set; } = string.Empty;
        public int KasanaTopTotal { get; set; }
        public int KasanaTopNoRead { get; set; }
        public decimal KasanaTopPct { get; set; }
        public int KasanaBelowTotal { get; set; }
        public int KasanaBelowNoRead { get; set; }
        public decimal KasanaBelowPct { get; set; }
    }
}
