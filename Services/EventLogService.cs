using System.Text.Json;
using DbBackupManager.Models;
using Microsoft.Extensions.Options;

namespace DbBackupManager.Services;

public class EventLogService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const int MaxEvents = 500;

    public EventLogService(IOptions<BackupRestoreOptions> options, IWebHostEnvironment env)
    {
        var path = options.Value.EventLogPath;
        _filePath = Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath, path);
    }

    public async Task<List<BackupRestoreEvent>> GetEventsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await ReadAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AppendEventAsync(BackupRestoreEvent evt)
    {
        await _semaphore.WaitAsync();
        try
        {
            var events = await ReadAsync();
            events.Insert(0, evt);
            if (events.Count > MaxEvents)
                events = events.Take(MaxEvents).ToList();
            await WriteAsync(events);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<List<BackupRestoreEvent>> ReadAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<BackupRestoreEvent>>(json, JsonOptions) ?? [];
    }

    private async Task WriteAsync(List<BackupRestoreEvent> events)
    {
        var json = JsonSerializer.Serialize(events, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
