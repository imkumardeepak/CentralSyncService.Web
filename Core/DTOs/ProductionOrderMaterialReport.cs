using System;

namespace Web.Core.DTOs
{
    public class ProductionOrderMaterialReport
    {
        public long OrderNo { get; set; }
        public string Batch { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public long OrderQty { get; set; }
        public long PrintedQty { get; set; }
        public long TotalTransferQty { get; set; }
        public long PendingToScan { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal CompletionPercent { get; set; }
    }
}
