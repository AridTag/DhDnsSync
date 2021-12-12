using DhDnsSync;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        services.Configure<DreamHostConfig>(config.GetSection(nameof(DreamHostConfig)));
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();