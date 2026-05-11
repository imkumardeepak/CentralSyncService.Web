using System;
using System.Collections.Generic;

namespace Web.Core.DTOs
{
    public class NoReadKasanaReadKomalReportResult
    {
        public DateTime SelectedDate { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public int TotalKasanaRead { get; set; }
        public int TotalKomalRead { get; set; }
        public int TotalNoReadAtKasanaReadAtKomal { get; set; }
        public List<NoReadKasanaReadKomalDetail> Details { get; set; } = new List<NoReadKasanaReadKomalDetail>();
    }

    public class NoReadKasanaReadKomalDetail
    {
        public int SerialNo { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string KasanaStatus { get; set; } = string.Empty;
        public string KomalStatus { get; set; } = string.Empty;
        public DateTime? FirstKomalScanTime { get; set; }
        public DateTime? LastKomalScanTime { get; set; }
    }
}
