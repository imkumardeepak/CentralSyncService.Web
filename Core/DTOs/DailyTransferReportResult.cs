using System.Collections.Generic;

namespace Web.Core.DTOs
{
    public class DailyTransferReportResult
    {
        public List<OverallDailyTransferRecord> Records { get; set; } = new();
        public DailyTransferMaterialTypeBreakdown MaterialBreakdown { get; set; } = new();
    }
}
