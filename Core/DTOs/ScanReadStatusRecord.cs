using System;

namespace Web.Core.DTOs
{
    public class ScanReadStatusRecord
    {
        public DateTime ReportDate { get; set; }
        public long TotalBoxes { get; set; }
        public long BothSideRead { get; set; }
        public long FromReadToNoRead { get; set; }
        public long FromNoReadToRead { get; set; }
        public long BothSideNoRead { get; set; }
        public long IncompleteOrMissing { get; set; }
    }
}
