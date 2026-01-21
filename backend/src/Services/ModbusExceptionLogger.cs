using System.Text;

namespace ModbusPerfTest.Backend.Services;

/// <summary>
/// Service for logging Modbus communication exceptions to file with size-based rotation.
/// </summary>
public class ModbusExceptionLogger
{
    private readonly string _logFilePath;
    private readonly long _maxLogFileSizeBytes;
    private readonly ILogger<ModbusExceptionLogger> _logger;
    private readonly object _fileLock = new();
    private int _exceptionCount = 0;

    public int ExceptionCount => _exceptionCount;

    public ModbusExceptionLogger(IConfiguration configuration, ILogger<ModbusExceptionLogger> logger)
    {
        var baseLogPath = configuration.GetValue<string>("ModbusExceptionLog:LogFilePath", "logs/modbus-exceptions.log");
        
        // Create timestamped log file for each restart
        var logDir = Path.GetDirectoryName(baseLogPath) ?? "logs";
        var logFileName = Path.GetFileNameWithoutExtension(baseLogPath);
        var logExtension = Path.GetExtension(baseLogPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        _logFilePath = Path.Combine(logDir, $"{logFileName}-{timestamp}{logExtension}");
        
        var maxLogFileSizeMB = configuration.GetValue<int>("ModbusExceptionLog:MaxLogFileSizeMB", 10);
        _maxLogFileSizeBytes = maxLogFileSizeMB * 1024L * 1024L;
        _logger = logger;

        // Ensure log directory exists
        try
        {
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                _logger.LogInformation("Created Modbus exception log directory: {LogDir}", logDirectory);
            }
            
            _logger.LogInformation("Modbus exception logging initialized. Log file: {LogFilePath}", _logFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create log directory for {LogFilePath}", _logFilePath);
        }
    }

    /// <summary>
    /// Logs a Modbus communication exception to file.
    /// </summary>
    public void LogException(Exception exception, string deviceKey, byte slaveId, ushort startAddress, ushort count, string operation = "ReadHoldingRegisters")
    {
        if (exception == null) return;

        try
        {
            lock (_fileLock)
            {
                // Check file size and rotate if needed
                RotateLogIfNeeded();

                // Format log entry
                var logEntry = FormatLogEntry(exception, deviceKey, slaveId, startAddress, count, operation);

                // Append to file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                
                // Increment exception counter
                Interlocked.Increment(ref _exceptionCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write Modbus exception to log file: {LogFilePath}", _logFilePath);
        }
    }

    /// <summary>
    /// Formats an exception as a log entry string.
    /// </summary>
    private string FormatLogEntry(Exception exception, string deviceKey, byte slaveId, ushort startAddress, ushort count, string operation)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"[{DateTime.UtcNow:O}] MODBUS EXCEPTION");
        sb.AppendLine($"  Device: {deviceKey}");
        sb.AppendLine($"  SlaveId: {slaveId}");
        sb.AppendLine($"  Operation: {operation}");
        sb.AppendLine($"  Address: {startAddress}, Count: {count}");
        sb.AppendLine($"  Exception Type: {exception.GetType().Name}");
        sb.AppendLine($"  Message: {exception.Message}");
        
        if (exception.InnerException != null)
        {
            sb.AppendLine($"  Inner Exception: {exception.InnerException.GetType().Name}");
            sb.AppendLine($"  Inner Message: {exception.InnerException.Message}");
        }
        
        sb.AppendLine($"  Stack Trace: {exception.StackTrace}");
        sb.AppendLine(new string('-', 80));

        return sb.ToString();
    }

    /// <summary>
    /// Rotates the log file if it exceeds the maximum size.
    /// </summary>
    private void RotateLogIfNeeded()
    {
        try
        {
            var fileInfo = new FileInfo(_logFilePath);
            if (!fileInfo.Exists) return;

            if (fileInfo.Length > _maxLogFileSizeBytes)
            {
                var oldLogPath = _logFilePath + ".old";
                
                // Delete old backup if exists
                if (File.Exists(oldLogPath))
                {
                    File.Delete(oldLogPath);
                }

                // Rename current log to .old
                File.Move(_logFilePath, oldLogPath);

                _logger.LogInformation(
                    "Rotated Modbus exception log: {LogFilePath} -> {OldLogPath} (size: {SizeMB:F2} MB)",
                    _logFilePath,
                    oldLogPath,
                    fileInfo.Length / (1024.0 * 1024.0)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate Modbus exception log file");
        }
    }
}
