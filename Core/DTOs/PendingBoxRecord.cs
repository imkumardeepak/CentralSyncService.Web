using System;
namespace Web.Core.DTOs
{
    public class PendingBoxRecord
    {
        public long Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? LineCode { get; set; }
        public string? FromPlant { get; set; }
        public DateTime? FromScanTime { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string? ToPlant { get; set; }
        public DateTime? ToScanTime { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string MatchStatus { get; set; } = string.Empty;
        public int AgeMinutes { get; set; }
    }
}
