using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Policy;
using System.Text.Json;
using System.Timers;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class DomainCacheOperations
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //ключ имя домена, значение список хостов
        public static ConcurrentDictionary<string, List<string>> DomainCache { get; set; } = new();
        public static ConcurrentDictionary<string, List<string>> DeletedDomains { get; set; } = new();
        static DomainCacheOperations()
        {

        }

        public static void Run()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await GetDomainsFromCacheAsync();

                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Ошибка в методе {method}() - {message}", nameof(GetDomainsFromCacheAsync), e.Message);
                    }
                    await Task.Delay(GlobalOptions.Settings.CacheUpdateIntervalSeconds * 1000);
                }
            });
        }

        public static bool TryAddDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return false;
            return DomainCache.TryAdd(domain.ToLower(), new());
        }

        public static async Task<bool> TryAddDomainUrlAsync(string domainUrl)
        {
            if (string.IsNullOrEmpty(domainUrl) || !domainUrl.ToLower().StartsWith("http")) return false;

            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(domainUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = new List<string>(content.Split('\n', StringSplitOptions.RemoveEmptyEntries));

                foreach (var domain in result) TryAddDomain(domain);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при получении списка доменов - {message}", ex.Message);
                return false;
            }

            return true;
        }


        public static List<string> GetDomains() => DomainCache.Keys.ToList();
        public static List<string> GetDeletedDomains() => DeletedDomains.Keys.ToList();

        private static async Task<bool> GetDomainsFromCacheAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();

            //формирум таблицу доменов для запросов
            var domainList = new List<string>();
            foreach (var cacheKey in DomainCache.Keys)
            {
                if (DomainCache.Keys.Any(t => cacheKey != t && cacheKey.Contains(t))) continue;
                domainList.Add(cacheKey);
            }

            if (domainList.Count == 0) return false;


            foreach (var domain in domainList)
            {
                var json = await PowershellApiClient.GetDomainAsJsonFromCacheAsync(domain);
                if (string.IsNullOrEmpty(json.Item2)) continue;
                if (json.Item1 == 1) continue;

                var hosts = DeserialyzeJson(json.Item2);
                if (hosts.Count == 0) continue;

                foreach (var host in hosts)
                {
                    if (DomainCache.TryAdd(host.ToLower(), new()))
                    {
                        Logger.Warn("В кеше доменов найден новый домен - {host}", host);
                    }
                }
            }

            sw.Stop();
            Logger.Info("Обновление кеша {amount} доменов завершено за {timer} мс", domainList.Count, sw.ElapsedMilliseconds);

            return true;
        }
        private static List<string> DeserialyzeJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            try
            {
                if (json.Contains('[') && json.Contains(']'))
                {
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new();
                }
                else
                {
                    var data = JsonSerializer.Deserialize<string>(json) ?? string.Empty;
                    return new() { data };
                }

            }
            catch (Exception e)
            {
                Logger.ForErrorEvent()
                    .Message("Ошибка в методе {method}() - {message}", nameof(DeserialyzeJson), e.Message)
                    .Exception(e)
                    .Property("json", json)
                    .Log();
            }

            return new();
        }

        /// <summary>
        /// Удаляем домены, если пришла такая команда
        /// </summary>
        /// <param name="domain"></param>
        public static void TryRemoveDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return;
            var keys = DomainCache.Keys.Where(t => t.Contains(domain)).ToList();
            foreach (var key in keys) DomainCache.TryRemove(key, out _);
            DeletedDomains.TryAdd(domain, new());
        }

        /// <summary>
        /// Финальное удаление доменов, которые были удалены с микротика
        /// </summary>
        /// <param name="finaldeletedDomains"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void TryRemoveDeletedDomains(List<string> finaldeletedDomains)
        {
            if (!finaldeletedDomains.Any()) return;
            foreach (var key in finaldeletedDomains) DeletedDomains.TryRemove(key, out _);
        }


    }
}
