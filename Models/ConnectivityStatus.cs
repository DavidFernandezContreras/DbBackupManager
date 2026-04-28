namespace DbBackupManager.Models;

public class ConnectivityStatus
{
    public bool SourceConnected { get; set; }
    public bool DestinationConnected { get; set; }
    public DateTime? LastChecked { get; set; }
    public string SourceLabel { get; set; } = "";
    public string DestinationLabel { get; set; } = "";
}
