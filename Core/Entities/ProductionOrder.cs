using System;

namespace Web.Core.Entities
{
    public class ProductionOrder
    {
        public int Id { get; set; }
        public int? OrderNo { get; set; }
        public string PlantCode { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public int? OrderQty { get; set; }
        public string UOM { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public DateTime? BsDate { get; set; }
        public int? CurQTY { get; set; }
        public int? BalQTY { get; set; }
        public string ComFlag { get; set; } = string.Empty;
        public DateTime? UploadDate { get; set; }
        public string? UploadTime { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateTime { get; set; }
        public decimal? Mrp { get; set; }
        public decimal? OverDelivery { get; set; }
        public decimal? UnderDelivery { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public string PackLine { get; set; } = string.Empty;
        public string? PackLine2 { get; set; }
    }
}
