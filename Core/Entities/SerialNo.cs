using System;

namespace Web.Core.Entities
{
    public class SerialNo
    {
        public string? OldSapcode { get; set; }
        public string? NewBatch { get; set; }
        public int? NewSerialNo { get; set; }
    }
}
