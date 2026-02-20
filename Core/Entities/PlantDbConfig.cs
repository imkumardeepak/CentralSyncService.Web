using System;

namespace Web.Core.Entities
{
    /// <summary>
    /// Configuration for a remote plant database
    /// </summary>
    public class PlantDbConfig
    {
        public int Id { get; set; }
        public string PlantCode { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string PlantType { get; set; } = string.Empty;  // "FROM" or "TO"
        public string IpAddress { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public int LastSyncCount { get; set; }
        public string? LastSyncStatus { get; set; }
    }
}
