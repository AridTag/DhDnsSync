namespace DhDnsSync;

public class DreamHostConfig
{
    public int UpdateIntervalMinutes { get; set; }
    public string ApiKey { get; set; }
    public List<DnsZone> Zones { get; set; }
}

public class DnsZone
{
    public string Name { get; set; }
    public List<DnsRecord> DnsRecords { get; set; }

    public string Qualify(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "@")
        {
            return Name;
        }

        return $"{name}.{Name}";
    }
}

public enum RecordUpdateMode
{
    EnsureExists,
    PublicIp
}

public enum DnsRecordType
{
    MX,
    TXT,
    CNAME,
    SRV,
    A
}

public class DnsRecord
{
    public RecordUpdateMode UpdateMode { get; set; }
    public DnsRecordType Type { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}