namespace ModbusPerfTest.Backend.Models;

public class RuntimeConfig
{
    public bool UseAsyncRead { get; set; }
    public bool UseAsyncNModbus { get; set; }
    public bool AllowConcurrentFrameReads { get; set; }
    public string DataStorageBackend { get; set; } = "SQLite";
    public int MinWorkerThreads { get; set; } = 22;
}
