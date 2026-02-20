using System;
namespace Web.Core.DTOs
{
    public class BulkToggleRequest
    {
        public int[] Ids { get; set; } = Array.Empty<int>();
        public bool IsActive { get; set; }
    }
}
