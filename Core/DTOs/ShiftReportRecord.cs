using System;
namespace Web.Core.DTOs
{
    public class ShiftReportRecord
    {
        public string ShiftName { get; set; } = string.Empty;
        public int TotalIssue { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        public int TotalReceipt { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
        public DateTime? ShiftStart { get; set; }
        public DateTime? ShiftEnd { get; set; }
    }
}
