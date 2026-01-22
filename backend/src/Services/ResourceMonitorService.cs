using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services
{
    public class ResourceMonitorService : BackgroundService
    {
        private readonly ILogger<ResourceMonitorService> _logger;
        private readonly Process _currentProcess;
        private DateTime _lastCpuTime;
        private TimeSpan _lastTotalProcessorTime;
        private SystemResourceMetrics _latestMetrics;
        private readonly object _metricsLock = new object();

        public ResourceMonitorService(ILogger<ResourceMonitorService> logger)
        {
            _logger = logger;
            _currentProcess = Process.GetCurrentProcess();
            _lastCpuTime = DateTime.UtcNow;
            _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
            _latestMetrics = new SystemResourceMetrics();
        }

        public SystemResourceMetrics GetLatestMetrics()
        {
            lock (_metricsLock)
            {
                return new SystemResourceMetrics
                {
                    CpuPercentage = _latestMetrics.CpuPercentage,
                    MemoryUsageMB = _latestMetrics.MemoryUsageMB,
                    Timestamp = _latestMetrics.Timestamp
                };
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ResourceMonitorService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, stoppingToken);
                    UpdateMetrics();
                }
                catch (TaskCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating resource metrics");
                }
            }

            _logger.LogInformation("ResourceMonitorService stopped");
        }

        private void UpdateMetrics()
        {
            try
            {
                // Calculate CPU percentage
                var currentTime = DateTime.UtcNow;
                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                var totalMsPassed = (currentTime - _lastCpuTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                var cpuPercentage = cpuUsageTotal * 100;

                // Get memory usage in MB
                _currentProcess.Refresh();
                var memoryUsageMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);

                // Update stored metrics
                lock (_metricsLock)
                {
                    _latestMetrics.CpuPercentage = Math.Round(cpuPercentage, 1);
                    _latestMetrics.MemoryUsageMB = Math.Round(memoryUsageMB, 1);
                    _latestMetrics.Timestamp = DateTime.UtcNow;
                }

                // Update for next iteration
                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update resource metrics");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            // Don't dispose _currentProcess - it's a shared reference
        }
    }
}
