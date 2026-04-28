using System.Diagnostics;
using DbBackupManager.Models;
using Microsoft.Extensions.Options;

namespace DbBackupManager.Services;

public class BackupRestoreExecutor
{
    private readonly BackupRestoreOptions _options;
    private readonly EventLogService _eventLog;
    private readonly ILogger<BackupRestoreExecutor> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsRunning => _semaphore.CurrentCount == 0;

    public BackupRestoreExecutor(
        IOptions<BackupRestoreOptions> options,
        EventLogService eventLog,
        ILogger<BackupRestoreExecutor> logger)
    {
        _options = options.Value;
        _eventLog = eventLog;
        _logger = logger;
    }

    /// <summary>
    /// Returns false immediately if another run is already in progress.
    /// </summary>
    public async Task<bool> TryRunAsync(bool isManual, CancellationToken ct = default)
    {
        if (!await _semaphore.WaitAsync(0, ct))
            return false;

        try
        {
            await RunAsync(isManual, ct);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RunAsync(bool isManual, CancellationToken ct)
    {
        var evt = new BackupRestoreEvent
        {
            StartedAt = DateTime.UtcNow,
            IsManual = isManual,
            SourceDatabase = $"{_options.Source.Host}/{_options.Source.Database}",
            DestinationDatabase = $"{_options.Destination.Host}/{_options.Destination.Database}"
        };

        _logger.LogInformation("Backup/restore started (manual={IsManual})", isManual);

        var tempDir = string.IsNullOrWhiteSpace(_options.TempBackupDir)
            ? Path.GetTempPath()
            : _options.TempBackupDir;
        var backupFile = Path.Combine(tempDir, $"pgbackup_{DateTime.UtcNow:yyyyMMddHHmmss}.dump");

        try
        {
            _logger.LogInformation("pg_dump → {File}", backupFile);
            var (dumpOk, dumpOutput) = await RunProcessAsync(
                _options.PgDumpPath,
                $"-h {_options.Source.Host} -p {_options.Source.Port} -U {_options.Source.User} " +
                $"-d {_options.Source.Database} -F c -f \"{backupFile}\"",
                _options.Source.Password,
                ct);

            if (!dumpOk)
                throw new InvalidOperationException($"pg_dump failed: {dumpOutput}");

            _logger.LogInformation("pg_restore ← {File}", backupFile);
            var (restoreOk, restoreOutput) = await RunProcessAsync(
                _options.PgRestorePath,
                $"-h {_options.Destination.Host} -p {_options.Destination.Port} -U {_options.Destination.User} " +
                $"-d {_options.Destination.Database} -c --if-exists -F c \"{backupFile}\"",
                _options.Destination.Password,
                ct);

            if (!restoreOk)
                throw new InvalidOperationException($"pg_restore failed: {restoreOutput}");

            evt.Success = true;
            _logger.LogInformation("Backup/restore completed successfully");
        }
        catch (Exception ex)
        {
            evt.Success = false;
            evt.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Backup/restore failed");
        }
        finally
        {
            evt.CompletedAt = DateTime.UtcNow;
            await _eventLog.AppendEventAsync(evt);

            if (File.Exists(backupFile))
            {
                try { File.Delete(backupFile); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp file {File}", backupFile); }
            }
        }
    }

    private static async Task<(bool success, string output)> RunProcessAsync(
        string executable, string arguments, string pgPassword, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo.EnvironmentVariables["PGPASSWORD"] = pgPassword;

        process.Start();

        // Start reads before waiting to avoid deadlocks on full buffers
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw;
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        return (process.ExitCode == 0, string.IsNullOrEmpty(stderr) ? stdout : stderr);
    }
}
