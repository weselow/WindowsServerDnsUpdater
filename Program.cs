using NLog;
using NLog.Web;
using Microsoft.EntityFrameworkCore;
using WindowsServerDnsUpdater.Data;
using WindowsServerDnsUpdater.Models;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var connectionString = builder.Configuration.GetConnectionString("SqliteLogs");
    LoggingDbContext.ConnectionString = connectionString ?? string.Empty;
    builder.Services.AddDbContext<LoggingDbContext>(options =>
        options.UseSqlite(connectionString));
    logger.Info("Sqlite для логов подключена.");

    builder.Services.AddRazorPages();
    var app = builder.Build();

    // Применение миграций при запуске
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
        dbContext.Database.Migrate(); // Применение миграций
    }
    GlobalOptions.Settings = LoggingDbOperations.GetSettings();

    app.UseStaticFiles();

    // Добавляем маршруты для Razor Pages на верхнем уровне
    app.MapRazorPages();

    // Асинхронный метод для обновления DNS записи через PowerShell
    app.MapGet("/api/dnsupdate", (string action, string hostname, string ipAddress, string domain) =>
    {
        logger.Info("Поступил запрос от Микротика: action {action}, hostname {hostname}, ip {ip}, domain {domain}", action, hostname, ipAddress, domain);

        // Проверка на допустимые действия
        if (action != "add" && action != "update" && action != "delete")
        {
            // Возврат 400 Bad Request
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode: 400, type: "text/plain");
        }

        var newJob = new JobRecord()
        {
            Action = action,
            Hostname = hostname,
            Ip = ipAddress,
            Domain = domain
        };

        DataCore.Jobs.Enqueue(newJob);
        
        return Results.Ok($"Task to add DNS record for {hostname}.{domain} ({action}) - added successfully.");
    });

    MikrotikOperations.Run();
    JobManager.Run();
    logger.Info("Сервисы MikrotikOperations и JobManager запущены");

    // Запуск приложения
    app.Run();
}
catch (Exception exception)
{
    // Логирование ошибок
    logger.Error(exception, "Программа остановлена из-за исключения");
    throw;
}
finally
{
    // Очищаем и останавливаем все процессы NLog
    NLog.LogManager.Shutdown();
}
