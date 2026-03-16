namespace Web.Core.DTOs
{
    public class TodayDashboardStats
    {
        public int TotalIssueCount { get; set; }
        public int TotalIssueRead { get; set; }
        public int TotalIssueNoRead { get; set; }

        public int TotalReceiptCount { get; set; }
        public int TotalReceiptRead { get; set; }
        public int TotalReceiptNoRead { get; set; }

        public int Deviation { get; set; }
    }
}
