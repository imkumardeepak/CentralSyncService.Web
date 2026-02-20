using System;

namespace Web.Core.Entities
{
    public class LineCodeLookup
    {
        public int Id { get; set; }
        public string MaterialNumber { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string Pcode { get; set; } = string.Empty;
        public string LineCode { get; set; } = string.Empty;
    }
}
