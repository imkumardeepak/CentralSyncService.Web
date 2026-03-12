namespace Web.Core.DTOs
{
    public class DailyTransferReportDto
    {
        public string OrderNo { get; set; }
        public string Batch { get; set; }
        public string MaterialSAPCode { get; set; }
        public string MaterialName { get; set; }

        // Issue Metrics
        public int IssueTotal { get; set; }
        public int IssueRead { get; set; }
        public int IssueNoRead { get; set; }

        // Receipt Metrics
        public int ReceiptTotal { get; set; }
        public int ReceiptRead { get; set; }
        public int ReceiptNoRead { get; set; }

        // Deviation
        public int Deviation { get; set; }
    }
}
