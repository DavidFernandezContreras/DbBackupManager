namespace DbBackupManager.Models;

public class DashboardViewModel
{
    public List<BackupRestoreEvent> Events { get; set; } = [];
    public ConnectivityStatus Connectivity { get; set; } = new();
    public DateTime? NextScheduledRun { get; set; }
    public bool IsRunning { get; set; }
    public string? Message { get; set; }
    public string? MessageType { get; set; } = "info";
}
