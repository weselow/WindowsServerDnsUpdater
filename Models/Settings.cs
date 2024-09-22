namespace WindowsServerDnsUpdater.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public string MikrotikIp { get; set; } = string.Empty;
        public string MikrotikLogin { get; set; } = string.Empty;
        public string MikrotikPassword { get; set; } = string.Empty;
        public string DefaultDomain { get; set; } = "jabc.loc";
    }
}
