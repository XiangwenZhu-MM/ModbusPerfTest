using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ModbusPerfTest.TestSimulator;
using NModbus;
using NModbus.Data;

Console.WriteLine("=== Modbus TCP Device Simulator ===");
Console.WriteLine("Reading configuration from device-config.json\n");

// Load device configuration
var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "device-config.json");
var normalizedPath = Path.GetFullPath(configPath);

if (!File.Exists(normalizedPath))
{
    Console.WriteLine($"ERROR: Configuration file not found at: {normalizedPath}");
    Console.WriteLine("Please ensure device-config.json exists in the project root.");
    return;
}

var jsonContent = await File.ReadAllTextAsync(normalizedPath);
var config = JsonSerializer.Deserialize<DeviceConfiguration>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (config == null || config.Devices == null || config.Devices.Count == 0)
{
    Console.WriteLine("ERROR: Invalid or empty configuration.");
    return;
}

Console.WriteLine($"Configuration loaded: {config.Devices.Count} devices");
Console.WriteLine("Devices:");
foreach (var device in config.Devices)
{
    Console.WriteLine($"  - Port {device.Port}, Slave ID {device.SlaveId}, Frames: {device.Frames.Count}");
}

Console.WriteLine("\nStarting simulated Modbus TCP devices...");
Console.WriteLine("Press Ctrl+C to stop\n");

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, args) =>
{
    args.Cancel = true;
    cancellationTokenSource.Cancel();
    Console.WriteLine("\nShutting down...");
};

var tasks = new List<Task>();

for (int i = 0; i < config.Devices.Count; i++)
{
    var device = config.Devices[i];
    tasks.Add(Task.Run(() => RunModbusDevice(device.Port, device.SlaveId, cancellationTokenSource.Token)));
}

await Task.WhenAll(tasks);
Console.WriteLine("All devices stopped.");

static async Task RunModbusDevice(int port, int deviceId, CancellationToken cancellationToken)
{
    try
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[Device {deviceId}] Listening on port {port}");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (listener.Pending())
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClient(client, deviceId, cancellationToken), cancellationToken);
            }
            else
            {
                await Task.Delay(10, cancellationToken);
            }
        }

        listener.Stop();
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Device {deviceId}] Error: {ex.Message}");
    }
}

static async Task HandleClient(TcpClient client, int deviceId, CancellationToken cancellationToken)
{
    try
    {
        Console.WriteLine($"[Device {deviceId}] Client connected from {client.Client.RemoteEndPoint}");
        
        var stream = client.GetStream();
        var buffer = new byte[260];

        while (!cancellationToken.IsCancellationRequested && client.Connected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        // Simple Modbus TCP response simulation
                        // In a real implementation, this would parse Modbus requests and generate proper responses
                        // For performance testing, we just need to respond with valid-looking data
                        
                        // Simulate processing delay (network + PLC processing time)
                        await Task.Delay(Random.Shared.Next(1, 10), cancellationToken);
                        
                        // Echo back a simple response
                        // In reality, this would be a proper Modbus TCP frame
                        var response = new byte[bytesRead];
                        Array.Copy(buffer, response, Math.Min(bytesRead, response.Length));
                        
                        // Modify function code to indicate response
                        if (bytesRead >= 8)
                        {
                            response[7] = 0x03; // Read Holding Registers response
                        }
                        
                        await stream.WriteAsync(response, 0, response.Length, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (Exception)
            {
                break;
            }
        }
        
        Console.WriteLine($"[Device {deviceId}] Client disconnected");
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Device {deviceId}] Client error: {ex.Message}");
    }
    finally
    {
        client?.Close();
    }
}
