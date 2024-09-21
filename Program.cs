using NLog;
using NLog.Web;
using System;
using Microsoft.EntityFrameworkCore;
using WindowsServerDnsUpdater;
using WindowsServerDnsUpdater.Data;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var connectionString = builder.Configuration.GetConnectionString("SqliteLogs");
    LoggingDbContext.ConnectionString = connectionString ?? string.Empty;
    builder.Services.AddDbContext<LoggingDbContext>(options =>
        options.UseSqlite(connectionString));
    logger.Info("Sqlite для логов подключена.");

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    builder.Services.AddRazorPages();
    var app = builder.Build();

    app.UseStaticFiles();

    // Добавляем маршруты для Razor Pages на верхнем уровне
    app.MapRazorPages();

    // Асинхронный метод для обновления DNS записи через PowerShell
    app.MapGet("/api/dnsupdate", async (string action, string hostname, string ipAddress, string domain) =>
    {
        logger.Info("Поступил запрос от Микротика: action {action}, hostname {hostname}, ip {ip}, domain {domain}", action, hostname, ipAddress, domain);

        // Проверка на допустимые действия
        if (action != "add" && action != "update" && action != "delete")
        {
            // Возврат 400 Bad Request
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode: 400, type: "text/plain");
        }

        // Выполнение асинхронной команды
        var result = await DataBox.Run(action, hostname, ipAddress, domain);

        // Проверка результата выполнения
        if (result.Item1 != 0)
        {
            logger.Error("Выполнение команды завершено с ошибкой: {message}", result.Item2);
            return Results.Problem($"Error executing action '{action}' on DNS: {result.Item2}",
                statusCode: 500, type: "text/plain");
        }

        logger.Info("Команда выполнена успешно: {message}", result.Item2);
        return Results.Ok($"DNS record for {hostname}.{domain} {action}d successfully.");
    });

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
