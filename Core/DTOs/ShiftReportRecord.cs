using System;
namespace Web.Core.DTOs
{
    public class ShiftReportRecord
    {
        public string Shift { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public int TotalQty { get; set; }
    }
}
