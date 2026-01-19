using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Service for logging heartbeat warnings to file with size-based rotation.
/// </summary>
public class HeartbeatLogger
{
    private readonly HeartbeatConfig _config;
    private readonly ILogger<HeartbeatLogger> _logger;
    private readonly object _fileLock = new();

    public HeartbeatLogger(HeartbeatConfig config, ILogger<HeartbeatLogger> logger)
    {
        _config = config;
        _logger = logger;

        // Ensure log directory exists
        try
        {
            var logDir = Path.GetDirectoryName(_config.LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
                _logger.LogInformation("Created log directory: {LogDir}", logDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create log directory for {LogFilePath}", _config.LogFilePath);
        }
    }

    /// <summary>
    /// Writes a drift event to the log file.
    /// </summary>
    public void LogDriftEvent(DriftEvent evt)
    {
        if (evt == null) return;

        try
        {
            lock (_fileLock)
            {
                // Check file size and rotate if needed
                RotateLogIfNeeded();

                // Format log entry
                var logEntry = FormatLogEntry(evt);

                // Append to file
                File.AppendAllText(_config.LogFilePath, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write drift event to log file: {LogFilePath}", _config.LogFilePath);
        }
    }

    /// <summary>
    /// Formats a drift event as a log entry string.
    /// </summary>
    private string FormatLogEntry(DriftEvent evt)
    {
        return $"[{evt.Timestamp:O}] [{evt.EventType}] {evt.Message} (Mono: {evt.MonoElapsedMs}ms, Wall: {evt.WallElapsedMs:F1}ms)";
    }

    /// <summary>
    /// Rotates the log file if it exceeds the maximum size.
    /// </summary>
    private void RotateLogIfNeeded()
    {
        try
        {
            var fileInfo = new FileInfo(_config.LogFilePath);
            if (!fileInfo.Exists) return;

            long maxSizeBytes = _config.MaxLogFileSizeMB * 1024L * 1024L;

            if (fileInfo.Length > maxSizeBytes)
            {
                var oldLogPath = _config.LogFilePath + ".old";
                
                // Delete old backup if exists
                if (File.Exists(oldLogPath))
                {
                    File.Delete(oldLogPath);
                }

                // Rename current log to .old
                File.Move(_config.LogFilePath, oldLogPath);

                _logger.LogInformation(
                    "Rotated log file: {LogFilePath} -> {OldLogPath} (size: {SizeMB:F2} MB)",
                    _config.LogFilePath,
                    oldLogPath,
                    fileInfo.Length / (1024.0 * 1024.0)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate log file: {LogFilePath}", _config.LogFilePath);
        }
    }
}
