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

        [Display(Name = "Укажите ваш DNS сервер (LDAP://your-dns-server)")]
        public string DnsServer { get; set; } = "LDAP://127.0.0.1";
        public bool IfUsePowerShell { get; set; } = false;
    }
}
