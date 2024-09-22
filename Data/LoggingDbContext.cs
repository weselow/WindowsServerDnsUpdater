using Microsoft.EntityFrameworkCore;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Data
{
    public sealed class LoggingDbContext : DbContext
    {
        public static string ConnectionString { get; set; } = string.Empty;
        public DbSet<LogRecord> LogEntries { get; set; }  // Таблица для логов
        public DbSet<Settings> Settings { get; set; } 

        public LoggingDbContext()
        {
            Database.EnsureCreated();
        }

        public LoggingDbContext(DbContextOptions<LoggingDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    throw new InvalidOperationException("The SqlLite ConnectionString property has not been initialized.");
                }

                optionsBuilder.UseSqlite(ConnectionString);
            }
        }

        // Настройка модели базы данных
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LogRecord>(entity =>
            {
                entity.HasKey(e => e.Id);  // Первичный ключ

                // Указываем обязательные поля
                entity.Property(e => e.Time).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Logger).IsRequired();
                entity.Property(e => e.Level).IsRequired();

                // Поле для исключений может быть NULL
                entity.Property(e => e.Exception).IsRequired(false);
            });
        }
    }
}
