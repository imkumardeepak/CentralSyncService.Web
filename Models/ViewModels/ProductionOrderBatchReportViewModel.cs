using System;
using System.Collections.Generic;
using Web.Core.DTOs;

namespace Web.Models.ViewModels
{
    public class ProductionOrderBatchReportViewModel
    {
        public List<ProductionOrderBatchReport> Reports { get; set; } = new();
        public ProductionOrderBatchSummary Summary { get; set; } = new();
        public string? PlantCode { get; set; }
        public string? BatchNo { get; set; }
        public string? OrderNo { get; set; }
        public DateTime Date { get; set; }
    }
}
