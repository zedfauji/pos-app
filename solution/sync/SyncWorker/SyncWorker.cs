using MagiDesk.SyncService;
using Microsoft.Extensions.Hosting;

namespace MagiDesk.SyncWorker;

public class SyncWorker : BackgroundService
{
    private readonly SyncEngine _engine;
    private readonly SyncConfig _config;
    private readonly SimpleLogger _log;

    public SyncWorker(SyncEngine engine, SyncConfig config, SimpleLogger log)
    {
        _engine = engine;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("SyncWorker started.");
        var interval = Math.Clamp(_config.SyncIntervalMinutes, 2, 60);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _engine.RunOnceAsync(_config, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.Error("ExecuteAsync loop error", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (TaskCanceledException) { }
        }
        _log.Info("SyncWorker stopping.");
    }
}
