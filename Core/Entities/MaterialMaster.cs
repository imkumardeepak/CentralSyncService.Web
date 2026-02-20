using System;

namespace Web.Core.Entities
{
    public class MaterialMaster
    {
        public int Id { get; set; }
        public string? MaterialNumber { get; set; }
        public string? MaterialDescription { get; set; }
        public string? MaterialType { get; set; }
        public string? Name2 { get; set; }
        public string? Plant { get; set; }
        public string? BaseUnitOfMeasure { get; set; }
        public double? GrossWeight { get; set; }
        public int? TotalShelfLife { get; set; }
        public DateTime? ModificationDate { get; set; }
        public int? PackingSize { get; set; }
        public string? Mrp { get; set; }
        public int? Nop { get; set; }
        public string? SerialProfile { get; set; }
        public double? NetWeight { get; set; }
        public string? WeightUnit { get; set; }
        public int? MaterialGroup { get; set; }
        public string? Division { get; set; }
        public string? GeneralItemCategoryGroup { get; set; }
        public long? EanUpc { get; set; }
        public string? AdditionUomBasicUom { get; set; }
        public string? AdditionalUom { get; set; }
        public int? CoversionFactorNum { get; set; }
        public int? CoversionFactorDen { get; set; }
        public string? SalesUnit { get; set; }
        public int? MinRemainShelfLife { get; set; }
        public string? Active { get; set; }
        public string? HsnCode { get; set; }
        public DateTime? CreationDate { get; set; }
        public string? ItemTax { get; set; }
        public string? AlarmZone { get; set; }
        public decimal? Tolerence { get; set; }
        public int? PackSize { get; set; }
        public string? Message { get; set; }
        public string? DistributionChannel { get; set; }
        public string? ProdInspMemo { get; set; }
        public double? Min_Gross_wt { get; set; }
        public double? Max_Gross_wt { get; set; }
    }
}
