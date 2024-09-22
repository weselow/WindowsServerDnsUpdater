using System.Collections.Concurrent;
using System.Diagnostics;
using NLog;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class JobManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static JobManager()
        {
        }

        public static bool Run()
        {
            Task.Run(RunJobsAsync);
            return true;
        }

        private static async Task RunJobsAsync()
        {
            while (true)
            {
                if (DataCore.Jobs.Count > 0)
                    Logger.Info("Найдено {amount} заданий на изменение DNS записей.", DataCore.Jobs.Count);

                while (DataCore.Jobs.TryDequeue(out var job))
                {
                    var result = (0, "");
                    if (GlobalOptions.Settings.IfUsePowerShell)
                    {
                        result = await PowershellApiClient.ExecuteJob(action: job.Action,
                            domain: job.Domain,
                            hostname: job.Hostname, 
                            ipAddress: job.Ip);
                    }
                    else
                    {
                        result = DnsApiClient.ExecuteJob(action: job.Action,
                            domain: job.Domain,
                            hostname: job.Hostname,
                            ipAddress: job.Ip);
                    }

                    if (result.Item1 != 0)
                    {
                        Logger.Error("Выполнение команды завершено с ошибкой: {message}", result.Item2);
                    }
                    else
                    {
                        Logger.Info("Команда выполнена успешно: {message}", result.Item2);
                    }
                }
                Thread.Sleep(5 * 1000);
            }
        }
    }
}
