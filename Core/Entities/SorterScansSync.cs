using System;

namespace Web.Core.Entities
{
    /// <summary>
    /// Represents a sync log entry in the central database (SorterScans_Sync table)
    /// </summary>
    public class SorterScansSync
    {
        public long Id { get; set; }
        public long SourceId { get; set; }
        public string ScanType { get; set; } = string.Empty;
        public string CurrentPlant { get; set; } = string.Empty;
        public string? PlantCode { get; set; }
        public string? LineCode { get; set; }
        public string? Batch { get; set; }
        public string? MaterialCode { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public DateTime ScanDateTime { get; set; }
        public bool IsRead { get; set; }
        public string? Shift { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime SyncedAt { get; set; }
    }
}
