using Microsoft.AspNetCore.Mvc.RazorPages;
using NLog;
using WindowsServerDnsUpdater.Data;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Pages
{
    public class IndexModel : PageModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public List<LogRecord> LogLines { get; set; } = [];
        public void OnGet()
        {
            LogLines = LoggingDbOperations.GetLogsAsync().GetAwaiter().GetResult();
        }
    }
}
