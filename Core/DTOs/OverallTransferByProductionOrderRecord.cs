namespace Web.Core.DTOs
{
    public class OverallTransferByProductionOrderRecord
    {
        public string OrderNo { get; set; }
        public string MaterialNumber { get; set; }
        public string MaterialDescription { get; set; }
        public string Batch { get; set; }
        public decimal OrderQty { get; set; }
        public decimal CurQTY { get; set; }
        public int IssueCount { get; set; }
        public int ReceiptCount { get; set; }
        public decimal Deviation { get; set; }
    }
}
