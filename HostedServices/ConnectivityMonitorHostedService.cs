using DbBackupManager.Services;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DbBackupManager.HostedServices;

public class ConnectivityMonitorHostedService : BackgroundService
{
    private readonly ConnectivityState _state;
    private readonly BackupRestoreOptions _backupOptions;
    private readonly int _intervalSeconds;
    private readonly ILogger<ConnectivityMonitorHostedService> _logger;

    public ConnectivityMonitorHostedService(
        ConnectivityState state,
        IOptions<BackupRestoreOptions> backupOptions,
        IOptions<ConnectivityOptions> connectivityOptions,
        ILogger<ConnectivityMonitorHostedService> logger)
    {
        _state = state;
        _backupOptions = backupOptions.Value;
        _intervalSeconds = connectivityOptions.Value.CheckIntervalSeconds;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var source = await CheckAsync(_backupOptions.Source, stoppingToken);
            var dest = await CheckAsync(_backupOptions.Destination, stoppingToken);
            _state.Update(source, dest);

            _logger.LogDebug("Connectivity — source:{Source} dest:{Dest}", source, dest);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<bool> CheckAsync(DbServerOptions server, CancellationToken appStopping)
    {
        var cs = $"Host={server.Host};Port={server.Port};Database={server.Database};" +
                 $"Username={server.User};Password={server.Password};Timeout=5;";
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(appStopping);
            cts.CancelAfter(TimeSpan.FromSeconds(6));

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !appStopping.IsCancellationRequested)
        {
            _logger.LogDebug("Check failed {Host}/{Db}: {Msg}", server.Host, server.Database, ex.Message);
            return false;
        }
    }
}
