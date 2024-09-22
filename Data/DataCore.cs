using System.Collections.Concurrent;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public static class DataCore
    {
        public static ConcurrentQueue<JobRecord> Jobs { get; set; } = new();
    }
}
