namespace DbBackupManager.Services;

public class BackupScheduleState
{
    private readonly object _lock = new();
    private DateTime? _nextScheduledRun;

    public DateTime? NextScheduledRun
    {
        get { lock (_lock) return _nextScheduledRun; }
        set { lock (_lock) _nextScheduledRun = value; }
    }
}
