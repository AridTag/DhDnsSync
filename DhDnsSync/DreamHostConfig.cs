namespace DhDnsSync;

public class DreamHostConfig
{
    public int UpdateIntervalMinutes { get; set; }
    public string ApiKey { get; set; }
    public string Zone { get; set; }
    public List<DnsRecord> DnsRecords { get; set; }

    public string Qualify(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "@")
        {
            return Zone;
        }
        
        return $"{name}.{Zone}";
    }
}