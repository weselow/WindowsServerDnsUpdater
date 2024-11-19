using NLog;
using System.Net;
using tik4net;
using tik4net.Objects;
using tik4net.Objects.Ip.DhcpServer;
using tik4net.Objects.Ip.Firewall;

namespace WindowsServerDnsUpdater.Data
{
    public class MikrotikApiClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _ipAddress;
        private readonly string _username;
        private readonly string _password;

        public MikrotikApiClient(string ipAddress, string username, string password)
        {
            _ipAddress = ipAddress;
            _username = username;
            _password = password;
        }

        public List<DhcpServerLease> GetDhcpLeases()
        {
            try
            {
                using var connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, _ipAddress, _username, _password);

                var leases = connection.LoadAll<DhcpServerLease>().ToList();

                return leases;
            }
            catch (Exception e)
            {
                Logger.Error(e,"Ошибка при получении данных от микротика - {message}", e.Message);
            }

            return new();
        }

        public List<FirewallAddressList> GetFirewallAddressList(string addressListName)
        {
            var vpnSites = new List<FirewallAddressList>();

            try
            {
                using var connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, _ipAddress, _username, _password);

                // Выполняем запрос на получение address-list с указанным именем
                vpnSites = connection.LoadList<FirewallAddressList>()
                    .Where(x => x.List == addressListName)
                    .Select(t=>t)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,"Ошибка при подключении к MikroTik или выполнении команды: {ex.Message}", ex.Message);
            }

            return vpnSites;
        }

        public bool AddDomainsToAddressList(List<string> domains, string addressListName)
        {
            if(!domains.Any()) return false;

            try
            {
                using var connection = ConnectionFactory.OpenConnection(TikConnectionType.Api, _ipAddress, _username, _password);
                
                foreach (var domain in domains)
                {
                    try
                    {       var addressListEntry = new FirewallAddressList
                            {
                                Address = domain,
                                List = addressListName,
                                Comment = $"Added from domain cache {domain}"
                            };
                            connection.Save(addressListEntry);
                          Logger.Info("Mikrotik: добавлен домен {domain} в список {addressListName}", domain, addressListName);
                        
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Ошибка при добавлении домена {domain} в {addressList} на микротик: {ex.Message}", domain, addressListName, ex.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Ошибка при подключении к MikroTik для добавления записей: {message}", e.Message);
                return false;
            }

            return true;
        }
    }
}