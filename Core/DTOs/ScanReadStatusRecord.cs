using System;

namespace Web.Core.DTOs
{
    public class ScanReadStatusRecord
    {
        public DateTime ReportDate { get; set; }
        public long IssueTotal { get; set; }
        public long IssueRead { get; set; }
        public long IssueNoRead { get; set; }
        public long ReceiptTotal { get; set; }
        public long ReceiptRead { get; set; }
        public long ReceiptNoRead { get; set; }
    }
}
