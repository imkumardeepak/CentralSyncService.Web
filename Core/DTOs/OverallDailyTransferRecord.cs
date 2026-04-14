using System;

namespace Web.Core.DTOs
{
    public class OverallDailyTransferRecord
    {
        public string ReportDate { get; set; } = string.Empty;
        public int IssueTotal { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }
        public int ReceiptTotal { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }
        public int Deviation { get; set; }
    }
}
