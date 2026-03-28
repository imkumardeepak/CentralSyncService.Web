using System;
namespace Web.Core.DTOs
{
    public class ShiftReportRecord
    {
        public string Shift { get; set; } = string.Empty;
        public string SAPCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public int CurQTY { get; set; }           // Printed Quantity
        public int TotalQtyInCs { get; set; }     // Transfer Quantity in Cases
        public decimal TotalQtyInMT { get; set; } // Transfer Quantity in MT
    }
}
