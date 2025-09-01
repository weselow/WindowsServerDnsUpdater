using System;
using System.Diagnostics;
using NLog;

namespace WindowsServerDnsUpdater.Data
{
    public static class PowershellApiClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string Powershell { get; set; } = string.Empty;

        static PowershellApiClient()
        {
            FindPowerShell7();
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

        public static async Task<(int, string)> ExecuteJobAsync(string action, string hostname, string ipAddress,
            string domain)
        {
            if (string.IsNullOrEmpty(action) 
                || string.IsNullOrEmpty(ipAddress) 
                || string.IsNullOrEmpty(domain) 
                || string.IsNullOrEmpty(hostname))

            {
                return (1, $"Одно из полей для команды передано пустым - action:'{action}', hostname:'{hostname}', ipAddress:'{ipAddress}', domain:'{domain}'.");
            }

            try
            {
                var script = "";

                // Проверка действия: добавление/изменение (удаление больше не поддерживается)
                if (action == "add" || action == "update")
                {
                    // Универсальная логика для add и update:
                    // 1. Проверяем существование записи
                    // 2. Если существует - обновляем IP и продлеваем TTL на 12 часов
                    // 3. Если не существует - создаем новую с TTL 12 часов
                    script = $"$record = Get-DnsServerResourceRecord -ZoneName '{domain}' -Name '{hostname}' -RRType A -ErrorAction SilentlyContinue; " +
                             $"if ($record) {{ " +
                             $"$newRecord = $record.Clone(); " +
                             $"$newRecord.RecordData.IPv4Address = '{ipAddress}'; " +
                             $"$newRecord.TimeToLive = '12:00:00'; " +
                             $"Set-DnsServerResourceRecord -ZoneName '{domain}' -OldInputObject $record -NewInputObject $newRecord; " +
                             $"Write-Host 'Запись {hostname}.{domain} обновлена с продлением TTL на 12 часов' " +
                             $"}} else {{ " +
                             $"Add-DnsServerResourceRecordA -ZoneName '{domain}' -Name '{hostname}' -IPv4Address '{ipAddress}' -TimeToLive '12:00:00'; " +
                             $"Write-Host 'Создана новая запись {hostname}.{domain} с TTL 12 часов' " +
                             $"}}";
                }
                else if (action == "delete")
                {
                    // Удаление записей больше не производим - просто логируем и возвращаем успех
                    Logger.Info("Запрос на удаление записи {hostname}.{domain} проигнорирован согласно новой политике", hostname, domain);
                    return (0, "Удаление записей отключено согласно новой политике");
                }
                else
                {
                    return (1, $"Неподдерживаемое действие: {action}. Поддерживаются только 'add' и 'update'.");
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
                if (!string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
                {
                    Logger.ForInfoEvent()
                        .Message("Команда для {hostname} завершена, standartOutput: {standartOutput}, errorOutput: {errorOutput} .", hostname, output, error)
                        .Property("cmd", script) 
                        .Log();
                }

                return (process.ExitCode, $"Standart output:\n{output}\n\nError output:\n{error}");

            }
            catch (Exception ex)
            {
                return (1, ex.ToString());
            }
        }

        public static async Task<(int, string)> GetDomainAsJsonFromCacheAsync(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return (1, string.Empty);

            try
            {
                var script = $"Get-DnsServerResourceRecord -ZoneName '.' -ComputerName 'localhost' | Where-Object {{ $_.HostName -like '*{domain}*' }} | Select-Object -ExpandProperty HostName | ConvertTo-Json -Depth 3";
                
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
                Logger.Info("Запускаем команду {cmd}", script);
                using var process = new Process();
                process.StartInfo = psi;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                if (!string.IsNullOrEmpty(error))
                {
                    Logger.Error("Результат команды {cmd} - output: {output}, error: {errorOutput}", script, output, error);
                }

                return (process.ExitCode, output);

            }
            catch (Exception ex)
            {
                return (1, ex.ToString());
            }
        }
    }
}
