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
    logger.Info("Sqlite ��� ����� ����������.");

    builder.Services.AddRazorPages();
    var app = builder.Build();

    // ���������� �������� ��� �������
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
        dbContext.Database.Migrate(); // ���������� ��������
    }
    GlobalOptions.Settings = LoggingDbOperations.GetSettings();

    app.UseStaticFiles();

    // ��������� �������� ��� Razor Pages �� ������� ������
    app.MapRazorPages();

    // ����������� ����� ��� ���������� DNS ������ ����� PowerShell
    app.MapGet("/api/dnsupdate", (string action, string hostname, string ipAddress, string domain) =>
    {
        logger.Info("�������� ������ �� ���������: action {action}, hostname {hostname}, ip {ip}, domain {domain}", action, hostname, ipAddress, domain);

        // �������� �� ���������� ��������
        if (action != "add" && action != "update" && action != "delete")
        {
            // ������� 400 Bad Request
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode: 400, type: "text/plain");
        }
        
        JobManager.AddJob(action, hostname, ipAddress, domain);
        
        return Results.Ok($"Task to add DNS record for {hostname}.{domain} ({action}) - added successfully.");
    });

    MikrotikOperations.Run();
    DomainCacheOperations.Run();
    JobManager.Run();

    logger.Info("������� MikrotikOperations � JobManager ��������");

    // ������ ����������
    app.Run();
}
catch (Exception exception)
{
    // ����������� ������
    logger.Error(exception, "��������� ����������� ��-�� ����������");
    throw;
}
finally
{
    // ������� � ������������� ��� �������� NLog
    NLog.LogManager.Shutdown();
}
