using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusPerfTest.Backend.Models;
using ModbusPerfTest.Backend.Services;

namespace ModbusPerfTest.Backend.Tests.Fixtures;

public class ServiceTestFixture
{
    public IServiceProvider ServiceProvider { get; private set; }
    public Mock<IModbusDriver> MockDriver { get; private set; }

    public ServiceTestFixture()
    {
        var services = new ServiceCollection();
        
        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"MockDriver:Enabled", "true"}
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Mock Modbus Driver
        MockDriver = new Mock<IModbusDriver>();
        services.AddSingleton(MockDriver.Object);

        // Services - Use real implementations for better testing
        services.AddSingleton<ScanTaskQueue>();
        services.AddSingleton<MetricCollector>();
        services.AddSingleton<ClockDriftService>();
        
        // Mock DataQualityService as it's complex to test
        var mockDataQualityService = new Mock<DataQualityService>();
        services.AddSingleton(mockDataQualityService.Object);

        services.AddSingleton<DeviceScanManager>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public DeviceScanManager GetDeviceScanManager()
    {
        return ServiceProvider.GetRequiredService<DeviceScanManager>();
    }

    public ScanTaskQueue GetScanTaskQueue()
    {
        return ServiceProvider.GetRequiredService<ScanTaskQueue>();
    }

    public MetricCollector GetMetricCollector()
    {
        return ServiceProvider.GetRequiredService<MetricCollector>();
    }

    public List<DeviceConfig> GetTestDevices()
    {
        return new List<DeviceConfig>
        {
            new DeviceConfig
            {
                IpAddress = "192.168.1.100",
                Port = 502,
                SlaveId = 1,
                Frames = new List<FrameConfig>
                {
                    new FrameConfig { StartAddress = 0, Count = 10, ScanFrequencyMs = 1000 }
                }
            },
            new DeviceConfig
            {
                IpAddress = "192.168.1.101",
                Port = 502,
                SlaveId = 2,
                Frames = new List<FrameConfig>
                {
                    new FrameConfig { StartAddress = 100, Count = 5, ScanFrequencyMs = 500 },
                    new FrameConfig { StartAddress = 200, Count = 3, ScanFrequencyMs = 2000 }
                }
            }
        };
    }
}