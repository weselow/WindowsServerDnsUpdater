using System.ComponentModel.DataAnnotations;

namespace WindowsServerDnsUpdater.Models
{
    public class Settings
    {
       

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

        [Display(Name = "Название Vpn Sites Address List ")]
        public string VpnSitesListName { get; set; } = "vpn_sites";


        [Display(Name = "Интервал регулярного запроса лизов от микротика(в секундах)")]
        public int LeaseUpdateDelaySeconds { get; set; } = 60;

        [Display(Name = "Интервал запросов доменов из кеша (в секундах)")]
        public int CacheUpdateIntervalSeconds { get; set; } = 5;

        [Display(Name = "Интервал запросов Address List к Mikrotik'у (в секундах)")]
        public int VpnSitesListUpdateDelaySeconds { get; set; } = 5;
    }
}
