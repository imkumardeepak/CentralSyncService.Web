using System;

namespace Web.Core.DTOs
{
    public class OverallTransferByProductionOrderRecord
    {
        public string OrderNo { get; set; } = string.Empty;
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public int OrderQty { get; set; }
        public int CurQTY { get; set; }
        public int IssueCount { get; set; }
        public int ReceiptCount { get; set; }
        public int Deviation { get; set; }
    }
}
