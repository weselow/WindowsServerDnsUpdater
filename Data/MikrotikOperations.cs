using NLog;
using System.Diagnostics;
using System.Timers;
using tik4net.Objects.Ip.Firewall;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class MikrotikOperations
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static Dictionary<string, JobRecord> Leases { get; set; } = new();
        static MikrotikOperations()
        {
        }

        public static bool Run()
        {
            //обновление dns записей из dhcp leases
            Task.Run(async () =>
            {
                while (true)
                {
                    UpdateLeases();
                    await Task.Delay(GlobalOptions.Settings.LeaseUpdateDelaySeconds * 1000);
                }
            });

            //обновление списка vpn sites
            Task.Run(async () =>
            {
                while (true)
                {
                    GetVpnSitesList();
                    await Task.Delay(GlobalOptions.Settings.VpnSitesListUpdateDelaySeconds * 1000);
                }
            });

            return true;
        }


        /// <summary>
        /// Обновляем список лизов
        /// </summary>
        private static void UpdateLeases()
        {
            try
            {
                UpdateLeasesTask();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Ошибка в методе {method}() - {message}", nameof(UpdateLeasesTask), exception.Message);
            }
        }
        private static void UpdateLeasesTask()
        {
            if (string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikIp)
                || string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikLogin))
            {
                Logger.Info("Запрос к микротику dhcp leases не производим, нет логина/пароля.");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            var client = new MikrotikApiClient(ipAddress: GlobalOptions.Settings.MikrotikIp,
                username: GlobalOptions.Settings.MikrotikLogin,
                password: GlobalOptions.Settings.MikrotikPassword);
            var leases = client.GetDhcpLeases();
            sw.Stop();
            Logger.Info("Получено {amount} dhcp lease записей от микротика за {timer} мс", leases.Count, sw.ElapsedMilliseconds);

            try
            {
                var counter = 0;
                var jobs = new List<JobRecord>();
                foreach (var lease in leases)
                {
                    //если данные не изменились, и прошло еще меньше часа, то этот лиз пропускаем
                    if (Leases.TryGetValue(lease.ActiveMacAddress, out var item))
                    {
                        if (item.Hostname == lease.HostName && item.Ip == lease.ActiveAddress &&
                            DateTime.Now < item.LastUpdated.AddSeconds(GlobalOptions.Settings.LeaseUpdateDelaySeconds))
                        {
                            continue;
                        }
                    }

                    //если что-то изменилось, то создаем задание,
                    //добавляем его в локальный словарь и в список задач
                    var job = new JobRecord()
                    {
                        Action = "add",
                        Domain = GlobalOptions.Settings.DefaultDomain,
                        Hostname = lease.HostName,
                        Ip = lease.ActiveAddress,
                        LastUpdated = DateTime.Now
                    };

                    //добавляем в словарь
                    if (!Leases.TryAdd(lease.ActiveMacAddress, job))
                    {
                        Leases[lease.ActiveMacAddress] = job;
                    }

                    //добавляем в очередь
                    jobs.Add(job);
                    counter++;
                }

                foreach (var job in jobs) JobManager.Jobs.Enqueue(job);

                if (counter > 0) Logger.Info("Отправили на добавление {amount} leases от микротика.", counter);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при разборе leases от микротика - {message}", ex.Message);
            }
            return;
        }


        /// <summary>
        /// Обновляем список vpn sites
        /// </summary>
        private static void GetVpnSitesList()
        {
            try
            {
                GetVpnSitesListTask();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Ошибка в методе {method}() - {message}", nameof(GetVpnSitesListTask), exception.Message);
            }
        }
        private static void GetVpnSitesListTask()
        {
            if (string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikIp)
                || string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikLogin))
            {

                Logger.Info("Запрос к микротику к AddressList {addressList} не производим, нет данных.", GlobalOptions.Settings.VpnSitesListName);
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            var client = new MikrotikApiClient(ipAddress: GlobalOptions.Settings.MikrotikIp,
                username: GlobalOptions.Settings.MikrotikLogin,
                password: GlobalOptions.Settings.MikrotikPassword);

            var vpnSitesList = client.GetFirewallAddressList(GlobalOptions.Settings.VpnSitesListName);

            sw.Stop();
            Logger.Info("Получено {amount} записей AddressList:{addressList} от микротика за {timer} мс", vpnSitesList.Count, GlobalOptions.Settings.VpnSitesListName, sw.ElapsedMilliseconds);


            //выбираем записи, которые будут удалены из AddressList
            var deletedDomains = DomainCacheOperations.GetDeletedDomains();
            var toDeleteRecords = new List<FirewallAddressList>();
            foreach (var domain in deletedDomains)
            {
                var list = vpnSitesList.Where(t => t.Address.Contains(domain)).ToList();
                toDeleteRecords.AddRange(list);
            }
            var finaldeletedDomains = client.RemoveDomainsFromAddressList(toDeleteRecords, GlobalOptions.Settings.VpnSitesListName);
            DomainCacheOperations.TryRemoveDeletedDomains(finaldeletedDomains);

            //получаем текущие записи в DNS кеше
            var currentDomains = DomainCacheOperations.GetDomains();

            //проходим по списку адресов и добавляем их в DNS кеш
            foreach (var vpnSite in vpnSitesList)
            {
                //динамические пропускаем
                if (vpnSite.Dynamic) continue;

                //если домен уже есть в кеше, то пропускаем
                if (currentDomains.Contains(vpnSite.Address.ToLower())) continue;
                if (deletedDomains.Contains(vpnSite.Address.ToLower())) continue;

                if (DomainCacheOperations.TryAddDomain(vpnSite.Address))
                {
                    Logger.Info("Добавлен новый домен для поиска в кеше: {host}", vpnSite.Address);
                }
            }

            //теперь в исходный адрес лист добавляем домены, которые там отсутствовали
            var newDomains = new List<string>();
            foreach (var domain in currentDomains)
            {
                if (vpnSitesList.Any(t => t.Address.ToLower() == domain)) continue;
                newDomains.Add(domain);
            }

            //отправляем записи на микротик
            client.AddDomainsToAddressList(newDomains, GlobalOptions.Settings.VpnSitesListName);


            sw.Stop();
            if (newDomains.Count > 0) Logger.Info("Отправили на добавление на микротик в AddressList {amount} доменов за {timer}.", newDomains.Count, sw.ElapsedMilliseconds);

            return;
        }
    }
}
