DhDnsSync is a .NET worker service that ensures the DNS for your dreamhost domain is configured exactly how you want it.

Modify the appsettings.json with the details for your domain. See example.appsettings.json as a template

A docker image can be obtained from [DockerHub](https://hub.docker.com/r/aridtag/dhdnssync)

If using Docker then you can pass configuration via environment variables, or map appsettings.json as a volume to /app/appsettings.json

Example docker environment variables
```
DreamHostConfig__UpdateIntervalMinutes=60
DreamHostConfig__ApiKey=<ApiKey>

DreamHostConfig__Zones__0__Name=example1.com
DreamHostConfig__Zones__0__DnsRecords__0__UpdateMode=PublicIp
DreamHostConfig__Zones__0__DnsRecords__0__Type=A
DreamHostConfig__Zones__0__DnsRecords__0__Name=@

DreamHostConfig__Zones__0__DnsRecords__1__UpdateMode=EnsureExists
DreamHostConfig__Zones__0__DnsRecords__1__Type=MX
DreamHostConfig__Zones__0__DnsRecords__1__Name=@
DreamHostConfig__Zones__0__DnsRecords__1__Value=0 example1-com.mail.protection.outlook.com

DreamHostConfig__Zones__1__Name=example2.com
DreamHostConfig__Zones__1__DnsRecords__0__UpdateMode=PublicIp
DreamHostConfig__Zones__1__DnsRecords__0__Type=A
DreamHostConfig__Zones__1__DnsRecords__0__Name=@

DreamHostConfig__Zones__1__DnsRecords__1__UpdateMode=EnsureExists
DreamHostConfig__Zones__1__DnsRecords__1__Type=MX
DreamHostConfig__Zones__1__DnsRecords__1__Name=@
DreamHostConfig__Zones__1__DnsRecords__1__Value=0 example2-com.mail.protection.outlook.com
```