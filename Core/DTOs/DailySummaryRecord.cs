using System;
namespace Web.Core.DTOs
{
    public class DailySummaryRecord
    {
        public DateTime ReportDate { get; set; }
        public int TotalIssue { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        public int TotalReceipt { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
    }
}
