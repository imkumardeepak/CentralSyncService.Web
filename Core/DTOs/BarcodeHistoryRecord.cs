using System;
namespace Web.Core.DTOs
{
    public class BarcodeHistoryRecord
    {
        public long Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? LineCode { get; set; }
        public string? FromPlant { get; set; }
        public string? FromScanTime { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string? ToPlant { get; set; }
        public string? ToScanTime { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string MatchStatus { get; set; } = string.Empty;
        public int? TransitTimeSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
