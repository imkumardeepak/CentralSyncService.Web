using System;

namespace Web.Core.DTOs
{
    public class ProductionOrderBatchReport
    {
        public string PlantCode { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        
        public long OrderQty { get; set; }
        public long PrintedQty { get; set; }
        public long TotalTransferQty { get; set; }
        
        public long PendingToScan { get; set; }
        public string Status { get; set; } = string.Empty;
        
        public decimal CompletionPercent { get; set; }
    }

    public class ProductionOrderBatchSummary
    {
        public long TotalOrders { get; set; }
        public long TotalOrderQty { get; set; }
        public long TotalPrinted { get; set; }
        public long TotalFromScanned { get; set; }
        public long TotalPending { get; set; }
    }

    public class OrderDetailByBatch
    {
        public long OrderId { get; set; }
        public long OrderNo { get; set; }
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public long OrderQty { get; set; }
        public long Pending { get; set; }
        public long PrintedQty { get; set; }
    }
}
