using NLog;
using tik4net;
using tik4net.Objects;
using tik4net.Objects.Ip.DhcpServer;

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
    }
}