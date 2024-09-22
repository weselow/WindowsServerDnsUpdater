namespace WindowsServerDnsUpdater.Models
{
    public class JobRecord
    {
        public string Action { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
    }
}
