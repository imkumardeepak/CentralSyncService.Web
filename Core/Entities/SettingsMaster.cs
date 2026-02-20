using System;

namespace Web.Core.Entities
{
    public class SettingsMaster
    {
        public int Id { get; set; }
        public string Plant { get; set; } = string.Empty;
        public string PlantCode { get; set; } = string.Empty;
        public string LineCode { get; set; } = string.Empty;
        public string? ProductScannerIP { get; set; }
        public int? ProductScannerPort { get; set; }
        public string? RejectionIP { get; set; }
        public int? RejectionPort { get; set; }
        public string? CartonScannerIP { get; set; }
        public int? CartonScannerPort { get; set; }
        public string? WeighmentIP { get; set; }
        public int? WeighmentPort { get; set; }
        public string? PrinterIP { get; set; }
        public int? PrinterPort { get; set; }
        public string? ProductRejection { get; set; }
        public string? WeightRejection { get; set; }
        public string? Wtype { get; set; }
        public string? Scnstart { get; set; }
        public string? Scnend { get; set; }
        public string? Rejscnstart { get; set; }
        public string? Rejscnend { get; set; }
        public string? Wtstart { get; set; }
        public string? Wtend { get; set; }
        public string? Typeofweight { get; set; }
        public string? Typeofscanner { get; set; }
        public string? Linefeed { get; set; }
        public string? Wtcaponrej { get; set; }
    }
}
