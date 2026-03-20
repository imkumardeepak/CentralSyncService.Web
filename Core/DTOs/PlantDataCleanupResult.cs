using System;

namespace Web.Core.DTOs
{
    public class PlantDataCleanupResult
    {
        public int PlantsProcessed { get; set; }
        public int TablesCleaned { get; set; }
        public int TotalRowsDeleted { get; set; }
        public DateTime CutoffDate { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
