using System;

namespace Web.Core.Entities
{
    /// <summary>
    /// Represents a scan record from local database (SorterScans table in PlantLineDB)
    /// </summary>
    public class SyncScanRecord
    {
        public long Id { get; set; }
        public string CurrentPlant { get; set; } = string.Empty;
        public string? PlantCode { get; set; }
        public string? LineCode { get; set; }
        public string? Batch { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public DateTime ScanDateTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? SyncedAt { get; set; }
        public bool IsRead { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
    }
}
