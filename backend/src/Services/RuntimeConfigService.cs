using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class RuntimeConfigService
{
    private RuntimeConfig _config;
    private readonly object _lock = new object();

    public RuntimeConfigService()
    {
        _config = new RuntimeConfig();
    }

    public void Initialize(RuntimeConfig config)
    {
        lock (_lock)
        {
            _config = config;
        }
    }

    public RuntimeConfig GetConfig()
    {
        lock (_lock)
        {
            return new RuntimeConfig
            {
                UseAsyncRead = _config.UseAsyncRead,
                UseAsyncNModbus = _config.UseAsyncNModbus,
                AllowConcurrentFrameReads = _config.AllowConcurrentFrameReads,
                DataStorageBackend = _config.DataStorageBackend,
                MinWorkerThreads = _config.MinWorkerThreads
            };
        }
    }

    public void UpdateConfig(RuntimeConfig config)
    {
        lock (_lock)
        {
            // Apply ThreadPool settings immediately if they changed
            if (config.MinWorkerThreads != _config.MinWorkerThreads)
            {
                ThreadPool.GetMinThreads(out int _, out int minIoThreads);
                if (ThreadPool.SetMinThreads(config.MinWorkerThreads, minIoThreads))
                {
                    Console.WriteLine($"Runtime ThreadPool update: MinWorkerThreads set to {config.MinWorkerThreads}");
                }
                else
                {
                    Console.WriteLine($"FAILED to update ThreadPool: MinWorkerThreads={config.MinWorkerThreads}");
                }
            }
            
            _config = config;
        }
    }
}
