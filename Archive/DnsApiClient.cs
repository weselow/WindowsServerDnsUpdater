using Microsoft.CodeAnalysis.Scripting;
using NLog;
using System;
using System.DirectoryServices;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Archive
{
#pragma warning disable CA1416
    /*
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
                Logger.Debug("Поступила задача создать/обновить dc: {domain}, hostname: {hostname}, ip:{ip}", domain, hostname, ipAddress);
                using var zone = new DirectoryEntry(BuildLdapPath(domain));

                // Поиск записи
                DirectoryEntry? existingRecord = null;

                foreach (DirectoryEntry record in zone.Children)
                {
                    if (!record.Name.Equals($"DC={hostname}", StringComparison.OrdinalIgnoreCase)) continue;
                    existingRecord = record;
                    break;
                }

                if (existingRecord != null)
                {
                    // Запись существует, обновляем IP-адрес
                    existingRecord.Properties["dnsRecord"][0] = CreateARecord(ipAddress);
                    existingRecord.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} обновлена успешно.", hostname);
                }
                else
                {
                    // Запись не найдена, создаём новую
                    var newRecord = zone.Children.Add($"DC={hostname}", "DnsNode");
                    newRecord.Properties["dnsRecord"].Value = CreateARecord(ipAddress);
                    newRecord.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} добавлена успешно.", hostname);
                }

                return (0, $"Запись DNS для {hostname} добавлена успешно.");
            }
            catch (Exception ex)
            {
               Logger.Error(ex,"Ошибка при добавлении DNS-записи - {message}", ex.Message);
               return (1, $"Ошибка при добавлении DNS-записи - {ex.Message}");
            }
        }
        static (int, string) DeleteDnsRecord(string domain, string hostname)
        {
            try
            {
                Logger.Debug("Поступила задача удалить запись -  dc: {domain}, hostname: {hostname}", domain, hostname);

                using DirectoryEntry zone = new DirectoryEntry(BuildLdapPath(domain));

                foreach (DirectoryEntry record in zone.Children)
                {
                    if (!record.Name.Equals($"CN={hostname}", StringComparison.OrdinalIgnoreCase)) continue;
                    zone.Children.Remove(record); // Передаем сам объект записи, а не его имя
                    zone.CommitChanges();
                    Logger.Info("Запись DNS для {hostname} удалена успешно.", hostname);
                    return (0, $"Запись DNS для {hostname} удалена успешно.");
                }

                Logger.Info("Запись DNS для {hostname} для удаления не найдена.", hostname);
                return (0, $"Запись DNS для {hostname} для удаления не найдена.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при удалении DNS-записи - {message}", ex.Message);
                return (1, $"Ошибка при удалении DNS-записи - {ex.Message}");
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
            var ldapPath = $"{GlobalOptions.Settings.DnsServer}/DC={domainName},CN=MicrosoftDNS,DC=DomainDnsZones,{domainDcString}";

            Logger.Debug("Сформирована строка подключения к DNS серверу: {path}", ldapPath);

            return ldapPath;
        }
        private static byte[] CreateARecord(string ipAddress)
        {
            byte[] ipBytes = System.Net.IPAddress.Parse(ipAddress).GetAddressBytes();

            // DNS Record format для записи типа A
            byte[] dnsRecord = new byte[16];

            // Установка типа записи: A (0x0001)
            dnsRecord[0] = 0x01;
            dnsRecord[1] = 0x00;

            // Установка класса записи: IN (0x0001)
            dnsRecord[2] = 0x01;
            dnsRecord[3] = 0x00;

            // Время жизни (TTL), можно задать, например, 3600 секунд (1 час)
            dnsRecord[4] = 0x10;
            dnsRecord[5] = 0x00;
            dnsRecord[6] = 0x00;
            dnsRecord[7] = 0x00;

            // Длина данных (4 байта для записи типа A)
            dnsRecord[8] = 0x04;
            dnsRecord[9] = 0x00;

            // IP-адрес (в формате 4 байта)
            Array.Copy(ipBytes, 0, dnsRecord, 10, ipBytes.Length);

            return dnsRecord;
        }

    }
    */

#pragma warning restore CA1416
}
