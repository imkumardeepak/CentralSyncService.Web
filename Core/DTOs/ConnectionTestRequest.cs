namespace Web.Core.DTOs
{
    public class ConnectionTestRequest
    {
        public string ServerIP { get; set; } = string.Empty;
        public int Port { get; set; } = 1433;
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
