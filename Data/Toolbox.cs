using NLog;

namespace WindowsServerDnsUpdater.Data
{
    public static class Toolbox
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static string ExtractWebsiteAddress(string url)
        {
            if (string.IsNullOrEmpty(url)|| !url.Contains("http")) return string.Empty;

            try
            {
                var uri = new Uri(url);

                // Получаем протокол и хост
                return uri.Host;
            }
            catch (Exception e)
            {
                Logger.Info(e, "Не смогли выделить домен из {url} - {message}", url, e.Message);
                return string.Empty;
            }
        }
    }
}
