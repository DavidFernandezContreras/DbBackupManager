using DbBackupManager.Services;
using Microsoft.Extensions.Options;

namespace DbBackupManager.HostedServices;

public class BackupRestoreHostedService : BackgroundService
{
    private readonly BackupRestoreExecutor _executor;
    private readonly BackupScheduleState _state;
    private readonly BackupRestoreOptions _options;
    private readonly ILogger<BackupRestoreHostedService> _logger;

    public BackupRestoreHostedService(
        BackupRestoreExecutor executor,
        BackupScheduleState state,
        IOptions<BackupRestoreOptions> options,
        ILogger<BackupRestoreHostedService> logger)
    {
        _executor = executor;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = CalculateNextRun(_options.GetScheduleTimeSpan());
            _state.NextScheduledRun = nextRun;

            _logger.LogInformation("Next backup scheduled at {NextRun:g} (local)", nextRun.ToLocalTime());

            try
            {
                await Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            await _executor.TryRunAsync(isManual: false, stoppingToken);
        }
    }

    private static DateTime CalculateNextRun(TimeSpan scheduleTime)
    {
        // ScheduleTime is treated as local time; compare against local now, store as UTC
        var nowLocal = DateTime.Now;
        var nextLocal = nowLocal.Date + scheduleTime;
        if (nextLocal <= nowLocal)
            nextLocal = nextLocal.AddDays(1);
        return nextLocal.ToUniversalTime();
    }
}
