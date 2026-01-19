using System.Text.Json;
using ModbusPerfTest.Backend.Models;
using Microsoft.Extensions.Configuration;

namespace ModbusPerfTest.Backend.Services;

public class DeviceConfigService
{
    private readonly IConfiguration _appConfiguration;
    private DeviceConfiguration? _configuration;

    public DeviceConfigService(IConfiguration configuration)
    {
        _appConfiguration = configuration;
    }

    public DeviceConfiguration? CurrentConfiguration => _configuration;

    public async Task<bool> LoadFromJsonAsync(string jsonContent)
    {
        try
        {
            _configuration = JsonSerializer.Deserialize<DeviceConfiguration>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            
            if (_configuration != null)
            {
                // Apply global AllowConcurrentFrameReads setting from appsettings.json to all devices
                var globalAllowConcurrent = _appConfiguration.GetValue<bool>("AllowConcurrentFrameReads", false);
                foreach (var device in _configuration.Devices)
                {
                    device.AllowConcurrentFrameReads = globalAllowConcurrent;
                }
            }
            
            return _configuration != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoadFromFileAsync(string filePath)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            return await LoadFromJsonAsync(jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR loading config: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return false;
        }
    }

    public List<DeviceConfig> GetDevices()
    {
        return _configuration?.Devices ?? new List<DeviceConfig>();
    }
}
