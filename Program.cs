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
    logger.Info("Sqlite ��� ����� ����������.");

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    builder.Services.AddRazorPages();
    var app = builder.Build();

    app.UseStaticFiles();

    // ��������� �������� ��� Razor Pages �� ������� ������
    app.MapRazorPages();

    // ����������� ����� ��� ���������� DNS ������ ����� PowerShell
    app.MapGet("/api/dnsupdate", async (string action, string hostname, string ipAddress, string domain) =>
    {
        logger.Info("�������� ������ �� ���������: action {action}, hostname {hostname}, ip {ip}, domain {domain}", action, hostname, ipAddress, domain);

        // �������� �� ���������� ��������
        if (action != "add" && action != "update" && action != "delete")
        {
            // ������� 400 Bad Request
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode: 400, type: "text/plain");
        }

        // ���������� ����������� �������
        var result = await DataBox.Run(action, hostname, ipAddress, domain);

        // �������� ���������� ����������
        if (result.Item1 != 0)
        {
            logger.Error("���������� ������� ��������� � �������: {message}", result.Item2);
            return Results.Problem($"Error executing action '{action}' on DNS: {result.Item2}",
                statusCode: 500, type: "text/plain");
        }

        logger.Info("������� ��������� �������: {message}", result.Item2);
        return Results.Ok($"DNS record for {hostname}.{domain} {action}d successfully.");
    });

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
