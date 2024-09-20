using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ����� ��� ���������� DNS ������ ����� PowerShell
app.MapGet("/api/dnsupdate", (string action, string hostname, string ipAddress, string domain) =>
{
    try
    {
        string script = "";

        // �������� ��������: ����������/��������� ��� ��������
        if (action == "add" || action == "update")
        {
            // ������������ ������� ��� ���������� ��� ��������� DNS ������
            script = $"Add-DnsServerResourceRecordA -ZoneName '{domain}' -Name '{hostname}' -IPv4Address '{ipAddress}'";
        }
        else if (action == "delete")
        {
            // ������������ ������� ��� �������� DNS ������
            script = $"Remove-DnsServerResourceRecord -ZoneName '{domain}' -Name '{hostname}' -RRType A -Force";
        }
        else
        {
            // ���������� Results.Content ��� �������� ��������� � �������� 400
            return Results.Problem("Invalid action. Use 'add', 'update', or 'delete'.", statusCode:400, type: "text/plain");
        }

        // ��������� �������� ��� ������� PowerShell
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

        // �������� ���������� �������� � �������
        if (process.ExitCode != 0)
        {
            // ���������� Results.Content ��� �������� ��������� �� ������ � �������� 500
            return Results.Problem($"Error executing action '{action}' on DNS: {error}", statusCode: 500, type: "text/plain");
        }

        return Results.Ok($"DNS record for {hostname}.{domain} {action}d successfully.");
    }
    catch (Exception ex)
    {
        // ����������� ������ � ������� ��������� � �������� 500
        Console.WriteLine($"An error occurred: {ex.Message}");
        return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500, type: "text/plain");
    }
});

// ������ ����������
app.Run();
