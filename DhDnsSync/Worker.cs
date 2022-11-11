using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DhDnsSync;

public class Worker : BackgroundService
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.dreamhost.com")
    };
    
    private readonly ILogger<Worker> _Logger;
    private readonly IOptions<DreamHostConfig> _Config;

    public Worker(ILogger<Worker> logger, IOptions<DreamHostConfig> config)
    {
        _Logger = logger;
        _Config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _Logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await DoUpdate(_Config.Value).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMinutes(_Config.Value.UpdateIntervalMinutes), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<string?> GetPublicIpAsync()
    {
        var retrievalHosts = new[] { "https://icanhazip.com/", "https://ipinfo.io/ip" };
        foreach (var host in retrievalHosts)
        {
            try
            {
                var response = await HttpClient.GetStringAsync(new Uri(host, UriKind.Absolute)).ConfigureAwait(false);
                if (!IPAddress.TryParse(response, out _))
                {
                    continue;
                }
                
                return response.Trim();
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private async Task DoUpdate(DreamHostConfig config)
    {
        string? publicIp = null;
        if (config.Zones.Any(z => z.DnsRecords.Any(d => d.UpdateMode == RecordUpdateMode.PublicIp)))
        {
            publicIp = await GetPublicIpAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(publicIp))
            {
                _Logger.LogError("Failed to retrieve public ip address. Update aborted.");
                return;
            }
        }
        
        List<DreamHostDnsRecord> existingDnsRecords;
        try
        {
            var dnsList = await GetDnsListAsync(config.ApiKey).ConfigureAwait(false);
            if (dnsList == null)
            {
                _Logger.LogError("Failed to retrieve dns listing. Are you sure your API key is valid?");
                return;
            }

            existingDnsRecords = dnsList;
        }
        catch(Exception ex)
        {
            _Logger.LogError(ex, "Failed to retrieve dns listing");
            return;
        }

        foreach (var zoneConfig in config.Zones)
        {
            _Logger.LogInformation("Processing zone {Zone}", zoneConfig.Name);
            foreach (var configuredRecord in zoneConfig.DnsRecords)
            {
                var result = await ProcessRemovalAsync(config.ApiKey, zoneConfig, configuredRecord, existingDnsRecords, publicIp).ConfigureAwait(false);
                if (result == DnsActionResult.Error)
                {
                    continue;
                }

                result = await ProcessAddAsync(config.ApiKey, zoneConfig, configuredRecord, existingDnsRecords, publicIp).ConfigureAwait(false);
            }
        }
    }

    private static DreamHostDnsRecord? Match(DnsZone zoneConfig, DnsRecord configuredRecord, IReadOnlyList<DreamHostDnsRecord> dreamHostDnsRecords)
    {
        return dreamHostDnsRecords.FirstOrDefault(d => d.record == zoneConfig.Qualify(configuredRecord.Name) && d.type == configuredRecord.Type.ToString());
    }

    private async Task<DnsActionResult> ProcessAddAsync(string apiKey, DnsZone zoneConfig, DnsRecord configuredRecord, IReadOnlyList<DreamHostDnsRecord> existingRecords, string? publicIp)
    {
        if (Match(zoneConfig, configuredRecord, existingRecords) != null)
        {
            return DnsActionResult.Skipped;
        }
        
        string valueToAdd;
        switch (configuredRecord.UpdateMode)
        {
            case RecordUpdateMode.EnsureExists:
                {
                    valueToAdd = configuredRecord.Value;
                    break;
                }

            case RecordUpdateMode.PublicIp:
                {
                    if (string.IsNullOrWhiteSpace(publicIp))
                    {
                        _Logger.LogError("Public ip is missing");
                        return DnsActionResult.Error;
                    }
                    
                    valueToAdd = publicIp;
                    break;
                }

            default:
                {
                    Debug.Fail($"Unhandled {nameof(RecordUpdateMode)}");
                    return DnsActionResult.Skipped;
                }
        }

        if (!await AddDnsRecordAsync(apiKey, zoneConfig.Qualify(configuredRecord.Name), configuredRecord.Type.ToString(), valueToAdd).ConfigureAwait(false))
        {
            return DnsActionResult.Error;
        }

        return DnsActionResult.Success;
    }

    private async Task<DnsActionResult> ProcessRemovalAsync(string apiKey, DnsZone zoneConfig, DnsRecord configuredRecord, List<DreamHostDnsRecord> existingRecords, string? publicIp)
    {
        var dreamHostRecord = Match(zoneConfig, configuredRecord, existingRecords);
        if (dreamHostRecord == default)
        {
            // Nothing to do.
            return DnsActionResult.Skipped;
        }
        
        switch (configuredRecord.UpdateMode)
        {
            case RecordUpdateMode.EnsureExists:
                {
                    // Nothing to do.
                    return DnsActionResult.Skipped;
                }

            case RecordUpdateMode.PublicIp:
                {
                    if (dreamHostRecord.value == publicIp)
                    {
                        return DnsActionResult.Skipped;
                    }
                    
                    if (await RemoveDnsRecordAsync(apiKey, dreamHostRecord))
                    {
                        existingRecords.Remove(dreamHostRecord);
                        return DnsActionResult.Success;
                    }
                    
                    _Logger.LogError("Failed to remove dns record {RecordName}", dreamHostRecord.record);
                    return DnsActionResult.Error;
                }

            default:
                {
                    Debug.Fail($"Unhandled {nameof(RecordUpdateMode)}");
                    return DnsActionResult.Error;
                }
        }
    }
    
    private async Task<List<DreamHostDnsRecord>?> GetDnsListAsync(string apiKey)
    {
        var response = await HttpClient.GetStringAsync($"/?key={apiKey}&format=json&cmd=dns-list_records").ConfigureAwait(false);
        var asDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
        if (asDict?.TryGetValue("result", out var successObj) != true || successObj.GetString() is not "success")
        {
            return null;
        }

        if (!asDict.TryGetValue("data", out var data))
        {
            return null;
        }

        return data.Deserialize<List<DreamHostDnsRecord>>();
    }
    
    private async Task<bool> AddDnsRecordAsync(string apiKey, string record, string type, string value)
    {
        string recordArgs = $"&record={record}&type={type}&value={value}";
        var response = await HttpClient.GetStringAsync($"/?key={apiKey}&format=json&cmd=dns-add_record{recordArgs}").ConfigureAwait(false);
        var asDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
        if (asDict?.TryGetValue("result", out var successObj) != true || successObj.GetString() is not "success")
        {
            return false;
        }
        
        return true;
    }

    private async Task<bool> RemoveDnsRecordAsync(string apiKey, DreamHostDnsRecord record)
    {
        string recordArgs = $"&record={record.record}&type={record.type}&value={record.value}";
        var response = await HttpClient.GetStringAsync($"/?key={apiKey}&format=json&cmd=dns-remove_record{recordArgs}").ConfigureAwait(false);
        var asDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
        if (asDict?.TryGetValue("result", out var successObj) != true || successObj.GetString() is not "success")
        {
            return false;
        }
        
        return true;
    }

    private enum DnsActionResult
    {
        Success,
        Skipped,
        Error
    }

    private class DreamHostDnsRecord
    {
        public string comment { get; set; }
        public string account_id { get; set; }
        public string value { get; set; }
        public string zone { get; set; }
        public string record { get; set; }
        public string type { get; set; }
        public string editable { get; set; }
    }
}