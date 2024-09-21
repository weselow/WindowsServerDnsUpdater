using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Net;
using System;
using NLog;

namespace WindowsServerDnsUpdater
{
    public static class DataBox
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string Powershell { get; set; } = string.Empty;

        static DataBox()
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
        public static async Task<(int, string)> Run(string action, string hostname, string ipAddress, string domain)
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
                var process = new Process { StartInfo = psi };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                Logger.Info("Команда для {hostname} завершена {cmd}", hostname, script);

                return (process.ExitCode, $"Standart output:\n{output}\n\nError output:\n{error}");

            }
            catch (Exception ex)
            {
                // Логирование ошибки и возврат сообщения с статусом 500
                return (1, ex.ToString());
            }
        }
        public static List<string> GetLast50LinesReversed()
        {
            // Определяем путь к файлу
            var logFilePath = Path.Combine(AppContext.BaseDirectory, "_logs", "log.txt");

            try
            {
                // Проверяем существование файла
                if (!File.Exists(logFilePath))
                {
                    Logger.Info("Файл не найден: {logFilePath}", logFilePath);
                    return [];
                }

                // Список для хранения строк
                var lines = new List<string>();

                // Открываем файл с доступом для чтения и записи, разрешая другим процессам доступ к файлу
                using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var streamReader = new StreamReader(fileStream))
                {
                    // Читаем файл построчно
                    while (streamReader.ReadLine() is { } line)
                    {
                        lines.Add(line);
                    }
                }

                // Возвращаем последние 50 строк в обратном порядке
                lines.Reverse();
                return lines.Take(50).ToList();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Ошибка при чтении логов - {message}", e.Message);
                return [];
            }
        }
    }
}
