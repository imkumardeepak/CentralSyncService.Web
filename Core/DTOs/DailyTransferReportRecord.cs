namespace Web.Core.DTOs
{
    public class DailyTransferReportRecord
    {
        // FROM Plant (Issue) Side
        public string FromPlant { get; set; } = string.Empty;
        
        public int IssueTotal { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        
        // TO Plant (Receipt) Side
        public string ToPlant { get; set; } = string.Empty;
        
        public int ReceiptTotal { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
        
        // Summary
        public int MatchedCount { get; set; }
        public int PendingToCount { get; set; }
    }
}
