using MagiDesk.SyncService;
using MagiDesk.SyncWorker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.SetBasePath(AppContext.BaseDirectory)
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = new SyncConfig();
        ctx.Configuration.Bind(config);
        services.AddSingleton(config);
        services.AddSingleton(new SimpleLogger(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MagiDesk", "sync", "sync.log")));
        services.AddSingleton<SyncEngine>();
        services.AddHostedService<SyncWorker>();
    })
    .Build();

await host.RunAsync();
