namespace Web.Core.DTOs
{
    public class DailyTransferReportRecord
    {
        // Total Production from BarcodePrint (HF Plant)
        public int TotalProduction { get; set; }
        
        // FROM Plant (Issue) Side - Overall Totals
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        
        // TO Plant (Receipt) Side - Overall Totals
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
    }
}
