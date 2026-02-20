using System;
using System.Collections.Generic;
using Web.Core.DTOs;

namespace Web.Models.ViewModels
{
    /// <summary>
    /// View model for the reporting dashboard
    /// </summary>
    public class DashboardViewModel
    {
        public List<DashboardStatsRecord> Stats { get; set; } = new List<DashboardStatsRecord>();
        public bool IsSyncRunning { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public int TotalFromSynced { get; set; }
        public int TotalToSynced { get; set; }
        public int TotalMatched { get; set; }
    }
}
