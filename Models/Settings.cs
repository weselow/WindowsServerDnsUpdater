using System.ComponentModel.DataAnnotations;

namespace WindowsServerDnsUpdater.Models
{
    public class Settings
    {
        internal int LeaseUpdateDelaySeconds;

        public int Id { get; set; }

        [Display(Name="IP Микротика")]
        public string MikrotikIp { get; set; } = string.Empty;

        [Display(Name = "Логин Микротика")]
        public string MikrotikLogin { get; set; } = string.Empty;

        [Display(Name = "Пароль Микротика")]
        [DataType(DataType.Password)]
        public string MikrotikPassword { get; set; } = string.Empty;

        [Display(Name = "Укажите вашу зону (example.com)")]
        public string DefaultDomain { get; set; } = "jabc.loc";
        
        [Display(Name = "Интервал обновления доменов из кеша (в секундах)")]
        public int CacheUpdateIntervalSeconds { get; set; } = 5;

        [Display(Name = "Название Vpn Sites Address List ")]
        public string VpnSitesListName { get; set; } = "vpn_sites";

        [Display(Name = "Интервал Mikrotik Address List (в секундах)")]
        public int VpnSitesListUpdateDelaySeconds { get; set; } = 5;
    }
}
