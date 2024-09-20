using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Метод для обновления DNS записи через PowerShell
app.MapGet("/api/dnsupdate", (string action, string hostname, string ipAddress, string domain) =>
{
    try
    {
        string script = "";

        // Проверка действия: добавление/изменение или удаление
        if (action == "add" || action == "update")
        {
            // Формирование команды для добавления или изменения DNS записи
            script = $"Add-DnsServerResourceRecordA -ZoneName '{domain}' -Name '{hostname}' -IPv4Address '{ipAddress}'";
        }
        else if (action == "delete")
        {
            // Формирование команды для удаления DNS записи
            script = $"Remove-DnsServerResourceRecord -ZoneName '{domain}' -Name '{hostname}' -RRType A -Force";
        }
        else
        {
            // Используем Results.Content для возврата сообщения с статусом 400
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode:400, type: "text/plain");
        }

        // Настройки процесса для запуска PowerShell
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-Command \"{script}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = psi };
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Проверка завершения процесса с ошибкой
        if (process.ExitCode != 0)
        {
            // Используем Results.Content для возврата сообщения об ошибке с статусом 500
            return Results.Problem($"Error executing action '{action}' on DNS: {error}", statusCode: 500, type: "text/plain");
        }

        return Results.Ok($"DNS record for {hostname}.{domain} {action}d successfully.");
    }
    catch (Exception ex)
    {
        // Логирование ошибки и возврат сообщения с статусом 500
        Console.WriteLine($"An error occurred: {ex.Message}");
        return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500, type: "text/plain");
    }
});

// Запуск приложения
app.Run();
