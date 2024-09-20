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

## Пояснение:
PowerShell команда: 
- Используется команда PowerShell Add-DnsServerResourceRecordA для добавления записи типа A в DNS сервер. Эта команда входит в состав модуля DnsServer, который должен быть установлен на сервере.
- Запуск PowerShell: PowerShell запускается через ProcessStartInfo, который позволяет выполнять команды непосредственно из C#.
- Обработка ошибок: Если PowerShell команда возвращает ошибку, она будет выведена в HTTP ответ с кодом ошибки 500.


### Альтернативный способ удаления старых записей из DNS сервера

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