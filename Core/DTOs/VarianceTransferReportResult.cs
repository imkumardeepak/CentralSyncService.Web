using System;
using System.Collections.Generic;

namespace Web.Core.DTOs
{
    public class VarianceTransferReportResult
    {
        public DateTime SelectedDate { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public int TotalProduction { get; set; }
        public int TotalTransfer { get; set; }
        public int MatchedProduction { get; set; }
        public int UnmatchedProduction { get; set; }
        public int TransferWithoutProduction { get; set; }
        public int Variance { get; set; }
        public List<VarianceTransferProducedDetail> ProducedDetails { get; set; } = new List<VarianceTransferProducedDetail>();
        public List<VarianceTransferExtraTransferDetail> ExtraTransferDetails { get; set; } = new List<VarianceTransferExtraTransferDetail>();
    }

    public class VarianceTransferProducedDetail
    {
        public int SerialNo { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string SapCode { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public string PackDescription { get; set; } = string.Empty;
        public DateTime? ProductionTime { get; set; }
        public int TransferCount { get; set; }
        public DateTime? FirstTransferTime { get; set; }
        public DateTime? LastTransferTime { get; set; }
        public bool IsMatched { get; set; }
    }

    public class VarianceTransferExtraTransferDetail
    {
        public string Barcode { get; set; } = string.Empty;
        public int TransferCount { get; set; }
        public DateTime? FirstTransferTime { get; set; }
        public DateTime? LastTransferTime { get; set; }
    }
}
