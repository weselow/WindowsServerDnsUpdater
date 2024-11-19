using System.Diagnostics;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using NLog;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class LoggingDbOperations
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static System.Timers.Timer ATimer { get; set; }
        private static DateTime LastVacuumRun { get; set; } = DateTime.MinValue;
        private static int VacuumIntervalMinutes { get; set; } = 24 * 60; // раз в сутки
        private static int MaxLogRecords { get; set; } = 100;

        private static IServiceProvider? _serviceProvider;



        static LoggingDbOperations()
        {
            //раз в 5 минут
            ATimer = new System.Timers.Timer(5 * 60 * 1000) { AutoReset = true, Enabled = true };
            ATimer.Elapsed += OnTimedEvent;
        }
        /// <summary>
        /// Передать сервисы DI в класс
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            EnsureCreated();
        }

        /// <summary>
        /// Создать подключение к БД
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static LoggingDbContext GetConnection()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider is not initialized.");

            // Создаем экземпляр DbContext
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
            // return _serviceProvider.GetRequiredService<LoggingDbContext>();
        }

        /// <summary>
        /// Включить WAL mode.
        /// </summary>
        private static void EnsureCreated()
        {
            while (true)
            {
                try
                {
                    using var db = GetConnection();
                    db.Database.EnsureCreated();
                    db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Ошибка в методе EnsureCreated() - {message}.", e.Message);
                }
                Thread.Sleep(5 * 1000);
            }

        }

        /// <summary>
        /// Раз в сутки делаем VACUUM базы данных и удаляем старые записи
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnTimedEvent(object? source, ElapsedEventArgs? e)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        await OnTimedEventTaskAsync();
                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 100)
                            Logger.Info("Время выполнения запроса {method}() к БД: {time} мс.", nameof(OnTimedEventTaskAsync), sw.ElapsedMilliseconds);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Ошибка в методе {method}() - {message}.", nameof(OnTimedEventTaskAsync), ex.Message);
                    }
                    await Task.Delay(5 * 1000);
                }
            });

        }
        private static async Task OnTimedEventTaskAsync()
        {
            await using var db = GetConnection();

            //Удаляем старые записи
            var count = await db.LogEntries.CountAsync();
            if (count > MaxLogRecords)
            {
                var toDelete = count - MaxLogRecords;
                var ids = await db.LogEntries
                    .OrderBy(t => t.Time)
                    .Take(toDelete)
                    .Select(t => t.Id)
                    .ToListAsync();
                // Удаляем все записи с полученными ID в один SQL-запрос
                var query = $"DELETE FROM LogEntries WHERE Id IN ({string.Join(",", ids)});";
                await db.Database.ExecuteSqlRawAsync(query);
            }

            // Проверяем, не пора ли выполнить VACUUM
            if (DateTime.Now > LastVacuumRun.AddMinutes(VacuumIntervalMinutes))
            {
                await db.Database.ExecuteSqlRawAsync("VACUUM;");
            }
        }

        /// <summary>
        /// Получить логи
        /// </summary>
        /// <returns></returns>
        public static async Task<List<LogRecord>> GetLogsAsync()
        {
            while (true)
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    var result = await GetLogsTaskAsync();
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 100)
                        Logger.Info("Время выполнения запроса {method} к БД: {time} мс.", nameof(GetLogsTaskAsync), sw.ElapsedMilliseconds);
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Ошибка в методе {method}() - {message}.", nameof(GetLogsTaskAsync), ex.Message);
                }
                Thread.Sleep(2 * 1000);
            }
        }
        private static async Task<List<LogRecord>> GetLogsTaskAsync()
        {
            await using var db = GetConnection();
            return await db.LogEntries
                .AsNoTracking()
                .OrderByDescending(t => t.Time)
                .Take(MaxLogRecords)
                .ToListAsync();
        }

        /// <summary>
        /// Получить лог по ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<LogRecord> GetLogByIdAsync(int id)
        {
            while (true)
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    var result = await GetLogByIdTaskAsync(id);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 100)
                        Logger.Info("Время выполнения запроса {method} к БД: {time} мс.", nameof(GetLogByIdTaskAsync), sw.ElapsedMilliseconds);
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Ошибка в методе {method}() - {message}.", nameof(GetLogByIdTaskAsync), ex.Message);
                }
                Thread.Sleep(2 * 1000);
            }
        }
        private static async Task<LogRecord> GetLogByIdTaskAsync(int id)
        {
            await using var db = GetConnection();
            return await db.LogEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id) ?? new LogRecord();
        }

        /// <summary>
        /// Получить настройки
        /// </summary>
        /// <returns></returns>
        public static Settings GetSettings()
        {
            while (true)
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    var result = GetSettingsTask();
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 100)
                        Logger.Info("Время выполнения запроса {method} к БД: {time} мс.",
                            nameof(GetSettingsTask), sw.ElapsedMilliseconds);
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Ошибка в методе {method}() - {message}.",
                        nameof(GetSettingsTask), ex.Message);
                }
                Thread.Sleep(2 * 1000);
            }
        }
        private static Settings GetSettingsTask()
        {
            using var db = GetConnection();
            return db.Settings.OrderBy(t=>t.Id).FirstOrDefault() ?? new Settings();
        }
    }
}
