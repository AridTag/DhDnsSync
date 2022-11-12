using DhDnsSync;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddEnvironmentVariables())
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        services.Configure<DreamHostConfig>(config.GetSection(nameof(DreamHostConfig)));
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();