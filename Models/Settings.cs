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

        [Display(Name = "Домен по-умолчанию")]
        public string DefaultDomain { get; set; } = "jabc.loc";
    }
}
