namespace Web.Core.DTOs
{
    public class DashboardStatsRecord
    {
        public string Period { get; set; } = string.Empty;
        public int TotalBoxes { get; set; }
        public int IssueTotal { get; set; }
        public int IssueNoRead { get; set; }
        public int ReceiptTotal { get; set; }
        public int ReceiptNoRead { get; set; }
    }
}
