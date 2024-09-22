using Microsoft.CodeAnalysis.Scripting;
using NLog;
using System;
using System.DirectoryServices;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
#pragma warning disable CA1416
    public static class DnsApiClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static (int, string) ExecuteJob(string action, string domain, string hostname, string ipAddress)
        {
            if (GlobalOptions.Settings.DnsServer.Length < 8)
            {
                Logger.Info("Не указан DNS сервер, запрос по API не делаем.");
                return (0, "Не указан DNS сервер, запрос по API не делаем.");
            }

            if (action == "add" || action == "update")
            {
                return UpdateOrCreateDnsRecord(domain, hostname, ipAddress);
            }

            return DeleteDnsRecord(domain, hostname);
        }
        static (int, string) UpdateOrCreateDnsRecord(string domain, string hostname, string ipAddress)
        {
            try
            {
                Logger.Info("Поступила задача создать/обновить dc: {domain}/ hostname: {hostname}/ ip:{ip}", domain, hostname, ipAddress);
                using var zone = new DirectoryEntry(BuildLdapPath(domain));

                // Поиск записи
                DirectoryEntry? existingRecord = null;

                foreach (DirectoryEntry record in zone.Children)
                {
                    if (!record.Name.Equals($"CN={hostname}", StringComparison.OrdinalIgnoreCase)) continue;
                    existingRecord = record;
                    break;
                }

                if (existingRecord != null)
                {
                    // Запись существует, обновляем IP-адрес
                    existingRecord.Properties["ARecord"][0] = ipAddress;
                    existingRecord.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} обновлена успешно.", hostname);
                }
                else
                {
                    // Запись не найдена, создаём новую
                    var newRecord = zone.Children.Add($"CN={hostname}", "MicrosoftDNS_A");
                    newRecord.Properties["ARecord"].Add(ipAddress);
                    newRecord.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} добавлена успешно.", hostname);
                }

                return (0, $"Запись DNS для {hostname} добавлена успешно.");
            }
            catch (Exception ex)
            {
               Logger.Error(ex,"Ошибка при добавлении DNS-записи - {message}", ex.Message);
               return (0, $"Ошибка при добавлении DNS-записи - {ex.Message}");
            }
        }
        static (int, string) DeleteDnsRecord(string domain, string hostname)
        {
            try
            {
                Logger.Info("Поступила задача удалить dc: {domain}/ hostname: {hostname}", domain, hostname);

                using DirectoryEntry zone = new DirectoryEntry(BuildLdapPath(domain));

                foreach (DirectoryEntry record in zone.Children)
                {
                    if (!record.Name.Equals($"CN={hostname}", StringComparison.OrdinalIgnoreCase)) continue;
                    zone.Children.Remove(record); // Передаем сам объект записи, а не его имя
                    zone.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} удалена успешно.", hostname);
                    return (0, $"Запись DNS для {{hostname}} удалена успешно.");
                }

                Logger.Info("Запись DNS для {hostname} не найдена.", hostname);
                return (0, $"Запись DNS для {hostname} не найдена.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при удалении DNS-записи - {message}", ex.Message);
                return (0, $"Ошибка при удалении DNS-записи - {ex.Message}");
            }
        }
        static string BuildLdapPath(string domainName)
        {
            // Разбиваем домен на части (subdomain.example.com -> ["subdomain", "example", "com"])
            var domainParts = domainName.Split('.');

            var zoneName = domainName;
            // Формируем часть строки с доменом в формате DC=part
            var domainDcString = string.Join(",", domainParts.Select(part => $"DC={part}"));

            // Формируем итоговый путь LDAP
            var ldapPath = $"{GlobalOptions.Settings.DnsServer}/CN={zoneName},CN=MicrosoftDNS,{domainDcString}";
            Logger.Info("Сформирована строка подключения к DNS серверу: {path}", ldapPath);

            return ldapPath;
        }

    }

#pragma warning restore CA1416
}
