using System;

namespace Web.Core.Entities
{
    public class LockProduct
    {
        public string? EanCode { get; set; }
        public string? SapCode { get; set; }
        public string? Flag { get; set; }
        public string? SyncFlag { get; set; }
    }
}
