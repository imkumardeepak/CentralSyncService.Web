using System;

namespace Web.Core.Entities
{
    public class BoxTracking
    {
        public long Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? LineCode { get; set; }
        public string? PlantCode { get; set; }
        public string? FromPlant { get; set; }
        public DateTime? FromScanTime { get; set; }
        public bool? FromFlag { get; set; }
        public string? FromRawData { get; set; }
        public DateTime? FromSyncTime { get; set; }
        public string? FromPCName { get; set; }
        public string? ToPlant { get; set; }
        public DateTime? ToScanTime { get; set; }
        public bool? ToFlag { get; set; }
        public string? ToRawData { get; set; }
        public DateTime? ToSyncTime { get; set; }
        public string? ToPCName { get; set; }
        public string MatchStatus { get; set; } = string.Empty;
        public int? TransitTimeSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
