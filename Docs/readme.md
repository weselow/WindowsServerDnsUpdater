## ������ ��� ��������������� ���������� ������� � DHCP ������� ���������.

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

## ���������:
PowerShell �������: 
- ������������ ������� PowerShell Add-DnsServerResourceRecordA ��� ���������� ������ ���� A � DNS ������. ��� ������� ������ � ������ ������ DnsServer, ������� ������ ���� ���������� �� �������.
- ������ PowerShell: PowerShell ����������� ����� ProcessStartInfo, ������� ��������� ��������� ������� ��������������� �� C#.
- ��������� ������: ���� PowerShell ������� ���������� ������, ��� ����� �������� � HTTP ����� � ����� ������ 500.


### �������������� ������ �������� ������ ������� �� DNS �������

������������� ���������� ������� Scavenging �� DNS �������
Windows DNS ������ ������������ �������� Scavenging, ������� ��������� ������������� ������� ������������ ������.

���� ��� ��������� Scavenging:
- �������� DNS Manager �� �������.
- �������� ������ ������� ���� �� ������� � �������� Set Aging/Scavenging for All Zones.
- �������� ����� Scavenge stale resource records.
- ���������� ��������� Refresh Interval � No-refresh Interval � ������������ � ������ ������������.

��� �������� Scavenging:
Scavenging ��������� ������� ������� ������, ������� �� ����������� � ������� ���������� �������.
���� �������� ����������� � ������������ �������, ��� �������� �������������� ����������.