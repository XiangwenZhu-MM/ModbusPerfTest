using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ModbusPerfTest.Backend.Services
{
    public class ThreadPoolMetrics
    {
        public int WorkerThreads { get; set; }
        public int CompletionPortThreads { get; set; }
        public long PendingWorkItems { get; set; }
        public int MinWorkerThreads { get; set; }
        public int MaxWorkerThreads { get; set; }
    }

    public class ScadaHealthMonitor
    {
        private readonly ILogger<ScadaHealthMonitor> _logger;
        private readonly int _alertThreshold;
        private readonly int _intervalMs;
        private CancellationTokenSource? _cts;
        private Task? _monitorTask;

        public ScadaHealthMonitor(ILogger<ScadaHealthMonitor> logger, int alertThreshold = 10, int intervalMs = 1000)
        {
            _logger = logger;
            _alertThreshold = alertThreshold;
            _intervalMs = intervalMs;
        }

        public ThreadPoolMetrics GetCurrentMetrics()
        {
            ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIo);
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);
            ThreadPool.GetMinThreads(out int minWorker, out int minIo);

            int activeWorker = maxWorker - availableWorker;
            int activeIo = maxIo - availableIo;

            long pending = 0;
            try
            {
                pending = ThreadPool.PendingWorkItemCount;
            }
            catch
            {
                // .NET version does not support PendingWorkItemCount
            }

            return new ThreadPoolMetrics
            {
                WorkerThreads = ThreadPool.ThreadCount,
                CompletionPortThreads = activeIo,
                PendingWorkItems = pending,
                MinWorkerThreads = minWorker,
                MaxWorkerThreads = maxWorker
            };
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _monitorTask?.Wait();
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var m = GetCurrentMetrics();
                _logger.LogInformation($"--- SCADA ENGINE HEALTH ---");
                _logger.LogInformation($"Active Threads: {m.WorkerThreads}");
                _logger.LogInformation($"Queue Length:   {m.PendingWorkItems} (Should be 0!)");
                _logger.LogInformation($"IO Threads:     {m.CompletionPortThreads}");
                _logger.LogInformation($"Pool Range:     {m.MinWorkerThreads} to {m.MaxWorkerThreads}");

                if (m.PendingWorkItems > _alertThreshold)
                    _logger.LogWarning($"WARNING: System is lagging! Check for blocking code. PendingWorkItems={m.PendingWorkItems}");

                await Task.Delay(_intervalMs, token);
            }
        }
    }
}
