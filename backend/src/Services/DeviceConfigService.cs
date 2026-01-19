using System.Text.Json;
using ModbusPerfTest.Backend.Models;

namespace ModbusPerfTest.Backend.Services;

public class DeviceConfigService
{
    private DeviceConfiguration? _configuration;

    public DeviceConfiguration? CurrentConfiguration => _configuration;

    public async Task<bool> LoadFromJsonAsync(string jsonContent)
    {
        try
        {
            _configuration = JsonSerializer.Deserialize<DeviceConfiguration>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
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
