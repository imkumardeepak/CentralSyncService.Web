using System;

namespace Web.Core.Entities
{
    public class OrderData
    {
        public string OrderNo { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public string? Batch { get; set; }
        public string? ProductDesc { get; set; }
        public int? Allok { get; set; }
        public decimal? WtRej { get; set; }
        public decimal? Total { get; set; }
    }
}
