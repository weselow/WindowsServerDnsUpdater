# WindowsServerDnsUpdater

Веб-приложение для автоматического управления DNS записями на Windows Server на основе данных DHCP от MikroTik роутеров.

## Архитектура решения

### Назначение
Проект обеспечивает интеграцию между MikroTik роутерами и Windows DNS Server, автоматически создавая и обновляя DNS записи при выдаче DHCP адресов.

### Технологический стек
- **ASP.NET Core 9** (Razor Pages)
- **Entity Framework Core** с SQLite для логирования
- **NLog** для логирования
- **tik4net** для работы с MikroTik API
- **PowerShell** для управления Windows DNS Server
- **C# 13.0** / **.NET 9**

## Структура проекта

```
WindowsServerDnsUpdater/
├── Data/
│   ├── DomainCacheOperations.cs     # Управление кешем доменов
│   ├── JobManager.cs                # Очередь задач DNS операций
│   ├── LoggingDbContext.cs          # EF контекст для логирования
│   ├── LoggingDbOperations.cs       # Операции с базой логов
│   ├── MikrotikApiClient.cs         # Клиент MikroTik API
│   ├── MikrotikOperations.cs        # Координация работы с MikroTik
│   ├── PowershellApiClient.cs       # Выполнение PowerShell команд
│   └── Toolbox.cs                   # Вспомогательные функции
├── Models/
│   ├── GlobalOptions.cs             # Глобальные настройки
│   ├── JobRecord.cs                 # Модель задачи DNS операции
│   ├── LogRecord.cs                 # Модель записи лога
│   └── Settings.cs                  # Модель настроек приложения
├── Pages/
│   ├── Index.cshtml/.cs             # Главная страница
│   ├── Settings.cshtml/.cs          # Страница настроек
│   ├── VpnSites.cshtml/.cs          # Управление VPN сайтами
│   └── About.cshtml/.cs             # Информация о приложении
├── Migrations/                      # EF миграции для SQLite
└── Program.cs                       # Точка входа приложения
```

## Основные компоненты

### 1. Web API эндпоинт
```csharp
app.MapGet("/api/dnsupdate", (string action, string hostname, string ipAddress, string domain) =>
{
    // Принимает запросы от MikroTik для управления DNS записями
    JobManager.AddJob(action, hostname, ipAddress, domain);
    return Results.Ok($"Task to add DNS record for {hostname}.{domain} ({action}) - added successfully.");
});
```

### 2. MikrotikApiClient
Подключается к MikroTik API для:
- Получения DHCP leases
- Работы с Firewall Address Lists
- Добавления/удаления доменов в Address Lists

### 3. JobManager
Асинхронная очередь задач для обработки DNS операций:
```csharp
public static ConcurrentQueue<JobRecord> Jobs { get; set; } = new();
```

### 4. PowershellApiClient
Выполняет команды PowerShell для управления Windows DNS Server:
- `Add-DnsServerResourceRecordA` - добавление записей **с TTL 12 часов**
- `Set-DnsServerResourceRecord` - обновление записей **с продлением TTL на 12 часов**
- `Get-DnsServerResourceRecord` - получение существующих записей
- **Удаление записей отключено** согласно новой политике

### 5. DomainCacheOperations
Управляет кешем доменов для мониторинга:
```csharp
public static ConcurrentDictionary<string, List<string>> DomainCache { get; set; } = new();
```

## Рабочий процесс

1. **DHCP событие на MikroTik** → отправка запроса на `/api/dnsupdate`
2. **JobManager** → добавление задачи в очередь
3. **PowershellApiClient** → выполнение DNS команды с TTL 12 часов
4. **Периодическая синхронизация** → обновление данных между MikroTik и DNS

## Политика управления DNS записями

### Добавление записей (action="add")
- **Проверяется существование записи** с таким же именем
- Если запись **существует**: обновляется IP адрес и **TTL продлевается на 12 часов**
- Если запись **не существует**: создается новая DNS запись с **TTL 12 часов**
- Команда: `Get-DnsServerResourceRecord` → `Set-DnsServerResourceRecord` или `Add-DnsServerResourceRecordA`

### Обновление записей (action="update")
- **Аналогична логике добавления**
- Проверяется существование записи
- Если запись **существует**: обновляется IP адрес и **TTL продлевается на 12 часов**
- Если запись **не существует**: создается новая с TTL 12 часов
- Команда: `Get-DnsServerResourceRecord` → `Set-DnsServerResourceRecord` или `Add-DnsServerResourceRecordA`

### Удаление записей (action="delete")
- **Удаление записей отключено** согласно новой политике
- Запросы на удаление логируются и игнорируются
- Записи автоматически удаляются по истечении TTL (12 часов)

## Стандарты кодирования

### Логирование
- Используется **NLog** для всех операций логирования
- Сообщения логов на русском языке
- Структурированное логирование с параметрами:
```csharp
Logger.Info("Получено {amount} dhcp lease записей от микротика за {timer} мс", leases.Count, sw.ElapsedMilliseconds);
```

### Асинхронность
- Все I/O операции выполняются асинхронно
- Используется `async/await` pattern
- Фоновые задачи через `Task.Run()`

### Обработка ошибок
- Глобальная обработка исключений
- Логирование всех ошибок
- Graceful degradation при недоступности внешних сервисов

### Dependency Injection
- Регистрация сервисов в `Program.cs`
- Использование встроенного DI контейнера ASP.NET Core

## Конфигурация

### appsettings.json
```json
{
  "ConnectionStrings": {
    "SqliteLogs": "Data Source=logs.db"
  }
}
```

### Настройки приложения (Settings.cs)
- IP и учетные данные MikroTik
- Настройки DNS зоны
- Интервалы обновления
- Название Address List для VPN сайтов

## Развертывание

1. Установить .NET 9 Runtime
2. Настроить подключение к MikroTik
3. Убедиться в наличии PowerShell модуля DnsServer
4. Запустить приложение
5. Настроить MikroTik для отправки DHCP событий

## Мониторинг

- Веб-интерфейс для просмотра логов
- Страница настроек для конфигурации
- Мониторинг VPN сайтов и доменного кеша
- Детальное логирование всех операций

## Безопасность

- Хранение паролей в зашифрованном виде
- Валидация входных параметров
- Использование HTTPS для внешних запросов
- Ограничение доступа к API эндпоинтам