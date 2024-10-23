using NLog;
using System.Diagnostics;
using System.Timers;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class MikrotikOperations
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static System.Timers.Timer ATimer { get; set; }
        private static Dictionary<string, JobRecord> Leases { get; set; } = new();
        private static int LeaseUpdateDelay { get; set; } = 60;

        static MikrotikOperations()
        {
            ATimer = new System.Timers.Timer(60 * 1000) { AutoReset = true, Enabled = true };
            ATimer.Elapsed += OnTimedEvent;

            OnTimedEvent(null, null);
        }

        public static bool Run() => true;

        private static void OnTimedEvent(object? source, ElapsedEventArgs? e)
        {
            try
            {
                UpdateLeases();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Ошибка в методе {method}() - {message}",
                    nameof(UpdateLeases), exception.Message);
            }
        }

        private static void UpdateLeases()
        {
            if (string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikIp)
                || string.IsNullOrEmpty(GlobalOptions.Settings.MikrotikLogin))
            {
                Logger.Info("Запрос к микротику leases не производим, нет данных.");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            var client = new MikrotikApiClient(ipAddress: GlobalOptions.Settings.MikrotikIp,
                username: GlobalOptions.Settings.MikrotikLogin,
                password: GlobalOptions.Settings.MikrotikPassword);
            var leases = client.GetDhcpLeases();
            sw.Stop();
            Logger.Info("Получено {amount} lease записей от микротика за {timer} мс", leases.Count, sw.ElapsedMilliseconds);

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
                            DateTime.Now < item.LastUpdated.AddMinutes(LeaseUpdateDelay))
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

                foreach (var job in jobs) DataCore.Jobs.Enqueue(job);

                if (counter > 0) Logger.Info("Отправили на добавление {amount} leases от микротика.", counter);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при разборе leases от микротика - {message}", ex.Message);
            }
            return;
        }
    }
}
