using DbBackupManager.Models;
using DbBackupManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DbBackupManager.Controllers;

public class HomeController : Controller
{
    private readonly EventLogService _eventLog;
    private readonly ConnectivityState _connectivity;
    private readonly BackupScheduleState _scheduleState;
    private readonly BackupRestoreExecutor _executor;
    private readonly BackupRestoreOptions _options;

    public HomeController(
        EventLogService eventLog,
        ConnectivityState connectivity,
        BackupScheduleState scheduleState,
        BackupRestoreExecutor executor,
        IOptions<BackupRestoreOptions> options)
    {
        _eventLog = eventLog;
        _connectivity = connectivity;
        _scheduleState = scheduleState;
        _executor = executor;
        _options = options.Value;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _eventLog.GetEventsAsync();
        var (sourceConnected, destConnected, lastChecked) = _connectivity.GetSnapshot();

        var vm = new DashboardViewModel
        {
            Events = events,
            Connectivity = new ConnectivityStatus
            {
                SourceConnected = sourceConnected,
                DestinationConnected = destConnected,
                LastChecked = lastChecked,
                SourceLabel = $"{_options.Source.Host}:{_options.Source.Port}/{_options.Source.Database}",
                DestinationLabel = $"{_options.Destination.Host}:{_options.Destination.Port}/{_options.Destination.Database}"
            },
            NextScheduledRun = _scheduleState.NextScheduledRun,
            IsRunning = _executor.IsRunning
        };

        if (TempData["Message"] is string msg)
        {
            var parts = msg.Split('|', 2);
            vm.Message = parts.Length == 2 ? parts[1] : msg;
            vm.MessageType = parts.Length == 2 ? parts[0] : "info";
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TriggerNow()
    {
        if (_executor.IsRunning)
        {
            TempData["Message"] = "warning|A backup & restore operation is already in progress.";
            return RedirectToAction(nameof(Index));
        }

        // Fire-and-forget: executor is a singleton with no request-scoped dependencies
        _ = _executor.TryRunAsync(isManual: true, CancellationToken.None);

        TempData["Message"] = "success|Backup & restore triggered. Refresh the page in a few minutes to see the result.";
        return RedirectToAction(nameof(Index));
    }

    [Route("error")]
    public IActionResult Error() =>
        Content("An unexpected error occurred. Please check the application logs.", "text/plain");
}
