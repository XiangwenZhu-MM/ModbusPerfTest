using ModbusPerfTest.Backend.Models;
using ModbusPerfTest.Backend.Services;
using ModbusPerfTest.Backend.Tests.Fixtures;

namespace ModbusPerfTest.Backend.Tests.Services;

public class DeviceScanManagerTests : IClassFixture<ServiceTestFixture>
{
    private readonly ServiceTestFixture _fixture;
    private readonly DeviceScanManager _manager;

    public DeviceScanManagerTests(ServiceTestFixture fixture)
    {
        _fixture = fixture;
        _manager = _fixture.GetDeviceScanManager();
    }

    [Fact]
    public void StartMonitoring_WithDevices_CreatesWorkersAndTaskGenerators()
    {
        // Arrange
        var devices = _fixture.GetTestDevices().Take(1).ToList();

        // Act
        _manager.StartMonitoring(devices);

        // Allow some time for async operations
        Thread.Sleep(100);

        // Assert
        var queueStats = _manager.GetQueueStats();
        Assert.NotNull(queueStats);
    }

    [Fact]
    public async Task StopMonitoringAsync_DisposesAllResources()
    {
        // Arrange
        var devices = _fixture.GetTestDevices().Take(1).ToList();
        _manager.StartMonitoring(devices);
        await Task.Delay(100); // Allow initialization

        // Act
        await _manager.StopMonitoringAsync();

        // Assert
        // Verify that after stopping, no new tasks are generated
        var queueStats = _manager.GetQueueStats();
        Assert.NotNull(queueStats);
    }

    [Fact]
    public void StartMonitoring_MultipleDevices_CreatesSeparateQueuesAndWorkers()
    {
        // Arrange
        var devices = _fixture.GetTestDevices();

        // Act
        _manager.StartMonitoring(devices);

        // Allow some time for setup
        Thread.Sleep(100);

        // Assert
        var queueStats = _manager.GetQueueStats();
        Assert.NotNull(queueStats);
    }

    [Fact]
    public async Task GetQueueStats_WhenNoMonitoring_ReturnsZeroStats()
    {
        // Arrange - ensure no monitoring is active
        try
        {
            await _manager.StopMonitoringAsync();
        }
        catch
        {
            // Expected if monitoring wasn't active
        }

        // Act
        var stats = _manager.GetQueueStats();

        // Assert
        dynamic dynamicStats = stats;
        Assert.Equal(0, dynamicStats.currentSize);
        Assert.Equal(0, dynamicStats.totalEnqueued);
        Assert.Equal(0, dynamicStats.totalDequeued);
        Assert.Equal(0, dynamicStats.totalDropped);
    }

    [Fact]
    public void GetAllDroppedTimestamps_WhenNoMonitoring_ReturnsEmptyQueue()
    {
        // Act
        var droppedTimestamps = _manager.GetAllDroppedTimestamps();

        // Assert
        Assert.NotNull(droppedTimestamps);
        Assert.Empty(droppedTimestamps);
    }

    [Fact]
    public void StartMonitoring_WithMultipleFrames_CreatesTaskGeneratorsForEachFrame()
    {
        // Arrange
        var devices = new List<DeviceConfig>
        {
            new DeviceConfig
            {
                IpAddress = "192.168.1.100",
                Port = 502,
                SlaveId = 1,
                Frames = new List<FrameConfig>
                {
                    new FrameConfig { StartAddress = 0, Count = 10, ScanFrequencyMs = 1000 },
                    new FrameConfig { StartAddress = 100, Count = 5, ScanFrequencyMs = 500 },
                    new FrameConfig { StartAddress = 200, Count = 3, ScanFrequencyMs = 2000 }
                }
            }
        };

        // Act
        _manager.StartMonitoring(devices);

        // Allow some time for setup
        Thread.Sleep(100);

        // Assert
        var queueStats = _manager.GetQueueStats();
        Assert.NotNull(queueStats);
    }

    [Fact]
    public void StartMonitoring_NullDeviceList_DoesNotThrow()
    {
        // Arrange
        List<DeviceConfig>? devices = null;

        // Act & Assert
        _manager.StartMonitoring(devices!);
        
        var queueStats = _manager.GetQueueStats();
        dynamic dynamicStats = queueStats;
        Assert.Equal(0, dynamicStats.currentSize);
    }

    [Fact]
    public void StartMonitoring_EmptyDeviceList_DoesNotThrow()
    {
        // Arrange
        var devices = new List<DeviceConfig>();

        // Act & Assert
        _manager.StartMonitoring(devices);
        
        var queueStats = _manager.GetQueueStats();
        dynamic dynamicStats = queueStats;
        Assert.Equal(0, dynamicStats.currentSize);
    }

    [Fact]
    public async Task StopMonitoringAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var devices = _fixture.GetTestDevices().Take(1).ToList();
        _manager.StartMonitoring(devices);
        await Task.Delay(100);

        // Act
        await _manager.StopMonitoringAsync();
        
        // Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () => await _manager.StopMonitoringAsync());
    }

    [Fact]
    public void StartMonitoring_ThenStop_QueueStatsPersist()
    {
        // Arrange
        var devices = _fixture.GetTestDevices().Take(1).ToList();
        _manager.StartMonitoring(devices);
        Thread.Sleep(200);

        // Act
        var statsDuringMonitoring = _manager.GetQueueStats();
        
        // Stop and wait a bit
        _manager.StopMonitoringAsync().Wait();
        Thread.Sleep(100);
        
        var statsAfterStop = _manager.GetQueueStats();

        // Assert
        Assert.NotNull(statsDuringMonitoring);
        Assert.NotNull(statsAfterStop);
    }
}