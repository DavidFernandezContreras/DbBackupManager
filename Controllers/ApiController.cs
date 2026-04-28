using DbBackupManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace DbBackupManager.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly ConnectivityState _connectivity;
    private readonly BackupRestoreExecutor _executor;
    private readonly BackupScheduleState _scheduleState;

    public ApiController(
        ConnectivityState connectivity,
        BackupRestoreExecutor executor,
        BackupScheduleState scheduleState)
    {
        _connectivity = connectivity;
        _executor = executor;
        _scheduleState = scheduleState;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var (source, dest, lastChecked) = _connectivity.GetSnapshot();
        return Ok(new
        {
            sourceConnected = source,
            destinationConnected = dest,
            lastChecked = lastChecked?.ToString("o"),
            isRunning = _executor.IsRunning,
            nextScheduledRun = _scheduleState.NextScheduledRun?.ToString("o")
        });
    }
}
