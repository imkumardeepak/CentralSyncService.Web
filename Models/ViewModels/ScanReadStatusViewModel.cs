using System;
using System.Collections.Generic;
using Web.Core.DTOs;

namespace Web.Models.ViewModels
{
    public class ScanReadStatusViewModel
    {
        public List<ScanReadStatusRecord> Reports { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
