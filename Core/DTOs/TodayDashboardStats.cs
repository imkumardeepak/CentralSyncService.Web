using System;
namespace Web.Core.DTOs
{
    public class TodayDashboardStats
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        
        public int TotalIssueCount { get; set; }
        public int TotalIssueRead { get; set; }
        public int TotalIssueNoRead { get; set; }

        public int TotalReceiptCount { get; set; }
        public int TotalReceiptRead { get; set; }
        public int TotalReceiptNoRead { get; set; }
    }
}
