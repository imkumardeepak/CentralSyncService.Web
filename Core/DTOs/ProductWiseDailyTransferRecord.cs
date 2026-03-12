namespace Web.Core.DTOs
{
    public class ProductWiseDailyTransferRecord
    {
        // Material Information
        public string MaterialDescription { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        
        // FROM Side (Issue) Metrics
        public int TotalIssue { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        
        // TO Side (Receipt) Metrics
        public int TotalReceipt { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
    }
}
