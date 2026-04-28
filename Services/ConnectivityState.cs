namespace DbBackupManager.Services;

public class ConnectivityState
{
    private readonly object _lock = new();
    private bool _sourceConnected;
    private bool _destinationConnected;
    private DateTime? _lastChecked;

    public void Update(bool source, bool destination)
    {
        lock (_lock)
        {
            _sourceConnected = source;
            _destinationConnected = destination;
            _lastChecked = DateTime.UtcNow;
        }
    }

    public (bool source, bool destination, DateTime? lastChecked) GetSnapshot()
    {
        lock (_lock)
            return (_sourceConnected, _destinationConnected, _lastChecked);
    }
}
