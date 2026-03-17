using System;
namespace Web.Core.DTOs
{
    public class BarcodeHistoryRecord
    {
        public long Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public string? LineCode { get; set; }
        public string ScanType { get; set; } = string.Empty;
        public string? CurrentPlant { get; set; }
        public string? ScanDateTime { get; set; }
        public string ReadStatus { get; set; } = string.Empty;
        public DateTime SyncedAt { get; set; }
    }
}
