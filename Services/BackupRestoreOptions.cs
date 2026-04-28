namespace DbBackupManager.Services;

public class BackupRestoreOptions
{
    public string ScheduleTime { get; set; } = "02:00:00";
    public DbServerOptions Source { get; set; } = new();
    public DbServerOptions Destination { get; set; } = new();
    public string EventLogPath { get; set; } = "backup_events.json";
    public string PgDumpPath { get; set; } = "pg_dump";
    public string PgRestorePath { get; set; } = "pg_restore";
    public string TempBackupDir { get; set; } = "";

    public TimeSpan GetScheduleTimeSpan() => TimeSpan.Parse(ScheduleTime);
}

public class DbServerOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ConnectivityOptions
{
    public int CheckIntervalSeconds { get; set; } = 30;
}
