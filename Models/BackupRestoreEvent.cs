namespace DbBackupManager.Models;

public class BackupRestoreEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsManual { get; set; }
    public string SourceDatabase { get; set; } = "";
    public string DestinationDatabase { get; set; } = "";

    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
}
