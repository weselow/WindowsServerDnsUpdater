## MCP Context7 Auto-Rules (эмуляция .windsurfrules для Visual Studio)

Всегда использовать MCP Context7 для вопросов, связанных с кодом, настройкой окружения, API и библиотеками.  
Под этим понимается следующее:

- Когда я запрашиваю примеры кода, конфигурацию или документацию — автоматически подключать **Context7**.
- Для работы с .NET использовать следующие библиотеки и их Context7 IDs:
  - ASP.NET MVC 9 → </microsoft/aspnetcore>
  - Entity Framework Core → </dotnet/efcore>
  - EFCore.BulkExtensions → </borisdj/efcore.bulkextensions>
  - RabbitMQ .NET Client → </rabbitmq/rabbitmq-dotnet-client>
  - Redis (StackExchange) → </stackexchange/stackexchange.redis>
  - NLog → </nlog/nlog>
- По умолчанию база данных — **Microsoft SQL Server 2022 Developer Edition**.
- Код всегда выдавать рабочий, в стиле C#, без псевдо-методов и пропусков.
- Включать необходимые секции конфигурации (`appsettings.json`, NLog.config и т. д.).
- При миграциях указывать команды `dotnet ef migrations add` и `dotnet ef database update`, выполняемые в проекте **DataLayer**.
- Для асинхронных операций использовать `async/await`, при необходимости применять `SemaphoreSlim` с таймаутами.
- Для очередей и внешних API — предлагать устойчивые шаблоны (retry/backoff через Polly, логирование ошибок, graceful shutdown).
- Не использовать реальные ключи/пароли — только переменные окружения или User Secrets.


## Шаблоны запросов для Copilot + Context7

### Entity Framework Core + BulkExtensions
```

Show example of batch insert and update with EF Core BulkExtensions in ASP.NET MVC 9.
Use Context7 docs: \</dotnet/efcore>, \</borisdj/efcore.bulkextensions>.
Provide: DbContext, entity with Guid PK, example controller action, and appsettings.json connection string for SQL Server 2022.

```

### EF Core миграции
```

Create code-first migration with EF Core for SQL Server 2022 (project DataLayer).
Use Context7: \</dotnet/efcore>.
Provide: entity, DbContext, migration command (dotnet ef migrations add), update command (dotnet ef database update).

```

### RabbitMQ
```

Implement publish/subscribe pattern with RabbitMQ in ASP.NET MVC 9.
Use Context7 docs: \</rabbitmq/rabbitmq-dotnet-client>.
Provide: connection setup, producer service, consumer service with background worker, DI registration, graceful shutdown.

```

### Redis (StackExchange.Redis)
```

Implement distributed cache and lock with StackExchange.Redis in ASP.NET MVC 9.
Use Context7 docs: \</stackexchange/stackexchange.redis>.
Provide: DI registration, example service methods, timeout handling with SemaphoreSlim.

```

### NLog
```

Configure NLog for ASP.NET MVC 9 with file + console targets.
Use Context7 docs: \</nlog/nlog>.
Provide: Program.cs logging setup, NLog.config, shutdown hook (NLog.LogManager.Shutdown()).

```

### Polly (для внешних API, RabbitMQ reconnect)
```

Add retry + circuit breaker policies with jitter for HttpClient in ASP.NET MVC 9.
Use Context7: \</app-vnext/polly>.
Provide: Program.cs with HttpClientFactory + AddPolicyHandler, policy examples, logging integration.

