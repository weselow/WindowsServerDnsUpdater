## Проблема
Предположим, на микротике работает DHCP сервер, а DNS сервер поднят на Windows Server.
- Windows DNS Server отвергает запросы от микротика на динамическое обновление записей, и предлагает костыль в виде третьей машины с Linux и nslookup.
- Это микро веб-приложение принимает от микротика http запросы и вносит записи в DNS сервер.
- Раз в минуту запрашивает у Микротика все текущие leases, и новые из них отправляет на добавление.
- Устанавливается в IIS.

## Пояснение:
Приложение использует PowerShell и API Микротика: 
- Используется команда PowerShell Add-DnsServerResourceRecordA для добавления записи типа A в DNS сервер. Эта команда входит в состав модуля DnsServer, который должен быть установлен на сервере.
- Запуск PowerShell: PowerShell запускается через ProcessStartInfo, который позволяет выполнять команды непосредственно из C#.
- Обработка ошибок: Если PowerShell команда возвращает ошибку, она будет выведена в HTTP ответ с кодом ошибки 500.
- Через АПИ Микротика получаем все текущие аренды ip адресов.
- Добавил опцию работать через DNS API. Выбирается в настройках.
- ОБРАТИТЬ ВНИМАНИЕ: путь LDAP формируется по базовому шаблону. Необходимо проверить его на соответствие вашей сети.


## Скрипт для автоматического добавления записей в DHCP сервере Микротика.

```
:local domain "jabc.loc"
:local dnsServer "10.20.0.151"
:local ip $"leaseActIP"
:local hostname $"lease-hostname"
:local action ""
:local result ""
:local url ""

# Check if IP address is assigned
:if ([:len $ip] = 0) do={
    :log error "No IP address assigned to the lease. Hostname: $hostname, Domain: $domain, DNS Server: $dnsServer"
    :error
}

# Determine action based on lease state
:if ($leaseBound = "1") do={
    # If the lease is newly bound or active, choose "add" or "update"
    :set action "add"  # By default, we are assuming 'add'. You can modify this to "update" based on your logic.
} else={
    # If the lease is expired or released, choose "delete"
    :set action "delete"
}

# Construct the URL with appropriate action
:set url "http://$dnsServer/api/dnsupdate?hostname=$hostname&ipAddress=$ip&domain=$domain&action=$action"

# Log variables before making the request
:log info "DNS update: Action: $action, Hostname: $hostname, IP: $ip, Domain: $domain, DNS Server: $dnsServer"

# Execute the request with error handling
:do {
    /tool fetch mode=http url=$url as-value output=user
    :set result ($"status")
    :log info "DNS $action successful. Result: $result"
} on-error={
    :log error "DNS $action failed. Hostname: $hostname, IP: $ip, Domain: $domain, DNS Server: $dnsServer"
}

```

## Альтернативный способ удаления старых записей из DNS сервера

Использование встроенной функции Scavenging на DNS сервере
Windows DNS сервер поддерживает механизм Scavenging, который позволяет автоматически удалять неактуальные записи.

Шаги для настройки Scavenging:
- Откройте DNS Manager на сервере.
- Щелкните правой кнопкой мыши на сервере и выберите Set Aging/Scavenging for All Zones.
- Включите опцию Scavenge stale resource records.
- Установите параметры Refresh Interval и No-refresh Interval в соответствии с вашими требованиями.

Как работает Scavenging:
Scavenging позволяет серверу удалять записи, которые не обновлялись в течение указанного времени.
Этот механизм применяется к динамическим записям, где включено автоматическое обновление.


## Настройка API Микротика
Да, чтобы MikroTik разрешил подключения по API, необходимо выполнить несколько шагов:

### 1. **Включить API-сервис**
   1. Подключитесь к MikroTik через WinBox или терминал.
   2. Перейдите в раздел **IP > Services**.
   3. Найдите сервис **API** и убедитесь, что он включен. Если нет, кликните правой кнопкой мыши и выберите **Enable**.

### 2. **Настроить права доступа**
   1. Перейдите в **System > Users**.
   2. Создайте нового пользователя или отредактируйте существующего.
   3. Убедитесь, что у пользователя есть права на доступ к API (например, добавьте группу **full** или создайте свою с нужными правами).

### 3. **Настроить Firewall (если необходимо)**
   Если на MikroTik настроены правила брандмауэра, убедитесь, что разрешён доступ к порту API (обычно это 8728 для TCP):
   1. Перейдите в **IP > Firewall > Filter Rules**.
   2. Создайте правило для разрешения трафика на порт 8728 (или 8729 для шифрованного API).

### 4. **Проверить IP-фильтрацию**
   Если включена фильтрация по IP, убедитесь, что ваш IP-адрес добавлен в разрешённый список.

После выполнения этих шагов вы сможете подключаться к MikroTik по API без проблем.