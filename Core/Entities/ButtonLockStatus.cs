using System;

namespace Web.Core.Entities
{
    public class ButtonLockStatus
    {
        public string ButtonName { get; set; } = string.Empty;
        public DateTime? LastClicked { get; set; }
    }
}
