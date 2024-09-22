using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Net;
using System;
using System.Collections.Concurrent;
using Microsoft.PowerShell.Commands;
using NLog;
using WindowsServerDnsUpdater.Models;
using static System.Collections.Specialized.BitVector32;

namespace WindowsServerDnsUpdater
{
    public static class DataBox
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string Powershell { get; set; } = string.Empty;
        public static ConcurrentQueue<JobRecord> Jobs { get; set; } = new();

        static DataBox()
        {
            FindPowerShell7();
            _ = RunJobsAsync();
        }

        private static bool FindPowerShell7()
        {
            // Предполагаемые пути для PowerShell 7
            string[] possiblePaths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PowerShell", "7", "pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe")
            ];

            // Проверяем существование pwsh.exe в возможных местах
            foreach (var path in possiblePaths)
            {
                if (!File.Exists(path)) continue;
                Logger.Info("PowerShell 7 найден по следующему пути: {path}", path);
                Powershell = path;
                return true;
            }

            Logger.Warn("Powershell 7 не был найден, выбран для использования Powershell 5.");
            Powershell = "powershell";
            return false;
        }

        private static async Task RunJobsAsync()
        {
            while (true)
            {
                if (Jobs.Count > 0)
                    Logger.Info("Найдено {amount} заданий на изменение DNS записей.", Jobs.Count);

                while (Jobs.TryDequeue(out var job))
                {
                    var result = await ExecuteJob(job.Action, job.Hostname, job.Ip, job.Domain);
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

        public static async Task<(int, string)> ExecuteJob(string action, string hostname, string ipAddress, string domain)
        {
            try
            {
                var script = "";

                // Проверка действия: добавление/изменение или удаление
                if (action == "add" || action == "update")
                {
                    // Формирование команды для добавления или изменения DNS записи
                    script = $"Add-DnsServerResourceRecordA -ZoneName '{domain}' -Name '{hostname}' -IPv4Address '{ipAddress}'";
                }
                else
                {
                    // Формирование команды для удаления DNS записи
                    script = $"Remove-DnsServerResourceRecord -ZoneName '{domain}' -Name '{hostname}' -RRType A -Force";
                }

                // Настройки процесса для запуска PowerShell
                var psi = new ProcessStartInfo
                {
                    FileName = Powershell,
                    Arguments = $"-Command \"{script}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Logger.Info("Запускаем команду для {hostname}: {cmd}", hostname, script);
                using var process = new Process();
                process.StartInfo = psi;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                Logger.Info("Команда для {hostname} завершена {cmd}", hostname, script);

                return (process.ExitCode, $"Standart output:\n{output}\n\nError output:\n{error}");

            }
            catch (Exception ex)
            {
                return (1, ex.ToString());
            }
        }

    }
}
