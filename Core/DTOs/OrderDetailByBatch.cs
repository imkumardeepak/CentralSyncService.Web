using System;

namespace Web.Core.DTOs
{
    public class OrderDetailByBatch
    {
        public long OrderId { get; set; }
        public long OrderNo { get; set; }
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public long OrderQty { get; set; }
        public long Pending { get; set; }
        public long PrintedQty { get; set; }
    }
}
