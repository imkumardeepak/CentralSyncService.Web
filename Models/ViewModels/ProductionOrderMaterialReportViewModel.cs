using System;
using System.Collections.Generic;
using Web.Core.DTOs;

namespace Web.Models.ViewModels
{
    public class ProductionOrderMaterialReportViewModel
    {
        public List<ProductionOrderMaterialReport> Reports { get; set; } = new();
        public List<string> PlantNames { get; set; } = new();
        public string? PlantName { get; set; }
        public string? MaterialCode { get; set; }
        public DateTime Date { get; set; }
    }
}
