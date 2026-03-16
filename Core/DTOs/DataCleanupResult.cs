using System;

namespace Web.Core.DTOs
{
    public class DataCleanupResult
    {
        public DateTime CutoffDate { get; set; }
        public int SorterScansDeleted { get; set; }
        public int BoxTrackingDeleted { get; set; }
    }
}
