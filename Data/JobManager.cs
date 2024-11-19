using System.Collections.Concurrent;
using System.Diagnostics;
using NLog;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class JobManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static ConcurrentQueue<JobRecord> Jobs { get; set; } = new();
        static JobManager()
        {
        }

        public static void Run()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await RunJobsAsync();
                    await Task.Delay(5 * 1000);
                }
            });
        }

        public static bool AddJob(string action, string hostname, string ipAddress, string domain)
        {
            var newJob = new JobRecord()
            {
                Action = action,
                Hostname = hostname,
                Ip = ipAddress,
                Domain = domain
            };

            Jobs.Enqueue(newJob);
            Task.Run(RunJobsAsync);
            return true;
        }

        private static async Task RunJobsAsync()
        {
            if (Jobs.Count > 0)
                Logger.Info("Найдено {amount} заданий на изменение DNS записей.", Jobs.Count);

            while (Jobs.TryDequeue(out var job))
            {
                var result = await PowershellApiClient.ExecuteJobAsync(action: job.Action,
                    domain: job.Domain,
                    hostname: job.Hostname,
                    ipAddress: job.Ip);
                if (result.Item1 != 0)
                {
                    Logger.Error("Выполнение команды завершено с ошибкой: {message}", result.Item2);
                }
            }
            return;
        }

       
    }
}
