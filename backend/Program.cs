using ModbusPerfTest.Backend.Services;
using ModbusPerfTest.Backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure thread pool minimum worker threads
ThreadPool.GetMinThreads(out int _, out int minIoThreads);
var minWorkerThreads = builder.Configuration.GetValue<int>("ThreadPoolMonitor:MinWorkerThreads", 22);
ThreadPool.SetMinThreads(minWorkerThreads, minIoThreads);
Console.WriteLine($"Thread pool configured: MinWorkerThreads={minWorkerThreads}");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register application services as singletons
builder.Services.AddSingleton<DataPointRepository>();
builder.Services.AddSingleton<DataPointBuffer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataPointBuffer>());
builder.Services.AddSingleton<DeviceConfigService>();
builder.Services.AddSingleton<ScanTaskQueue>();
builder.Services.AddSingleton<MetricCollector>();

// Register Modbus driver based on configuration
// Set "UseHighPerformanceDriver" to true to use the high-performance async driver (recommended)
var useHighPerformanceDriver = builder.Configuration.GetValue<bool>("UseHighPerformanceDriver", true);

if (useHighPerformanceDriver)
{
    Console.WriteLine("Using HIGH-PERFORMANCE Modbus driver (NModbus TCP with async operations)");
    builder.Services.AddSingleton<IModbusDriver, HighPerformanceModbusDriver>();
}
else
{
    Console.WriteLine("Using STANDARD Modbus driver (NModbus TCP)");
    builder.Services.AddSingleton<IModbusDriver, ModbusDriver>();
}

builder.Services.AddSingleton<ClockDriftService>();
builder.Services.AddSingleton<DataQualityService>();
builder.Services.AddSingleton<DataPointRepository>();
builder.Services.AddSingleton<DeviceScanManager>();

// Configure HeartbeatMonitor
var heartbeatConfig = builder.Configuration.GetSection("HeartbeatMonitor").Get<HeartbeatConfig>() ?? new HeartbeatConfig();
try
{
    heartbeatConfig.Validate();
    builder.Services.AddSingleton(heartbeatConfig);
    Console.WriteLine($"Heartbeat monitor configured: Enabled={heartbeatConfig.Enabled}, Interval={heartbeatConfig.IntervalMs}ms, Threshold={heartbeatConfig.ThresholdMs}ms");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Invalid HeartbeatMonitor configuration: {ex.Message}");
    Console.WriteLine("Using default configuration.");
    heartbeatConfig = new HeartbeatConfig();
    builder.Services.AddSingleton(heartbeatConfig);
}

// Register HeartbeatMonitor as hosted service and singleton for API access
builder.Services.AddSingleton<HeartbeatLogger>();
builder.Services.AddSingleton<HeartbeatMonitor>();
builder.Services.AddHostedService<HeartbeatMonitor>(sp => sp.GetRequiredService<HeartbeatMonitor>());

// Register ScadaHealthMonitor
builder.Services.AddSingleton<ScadaHealthMonitor>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ScadaHealthMonitor>>();
    var config = sp.GetRequiredService<IConfiguration>();
    int alertThreshold = config.GetValue<int>("ThreadPoolMonitor:AlertThreshold", 10);
    int intervalMs = config.GetValue<int>("ThreadPoolMonitor:IntervalMs", 1000);
    return new ScadaHealthMonitor(logger, alertThreshold, intervalMs);
});

var app = builder.Build();

// Start thread pool health monitor
var healthMonitor = app.Services.GetRequiredService<ScadaHealthMonitor>();
healthMonitor.Start();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Load device configuration from file on startup
var configService = app.Services.GetRequiredService<DeviceConfigService>();
var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "device-config.json");
var normalizedPath = Path.GetFullPath(configPath);

if (File.Exists(normalizedPath))
{
    Console.WriteLine($"Loading device configuration from: {normalizedPath}");
    var loaded = await configService.LoadFromFileAsync(normalizedPath);
    if (loaded)
    {
        Console.WriteLine($"Configuration loaded successfully. Devices: {configService.GetDevices().Count}");
        
        // Automatically start monitoring
        var scanManager = app.Services.GetRequiredService<DeviceScanManager>();
        scanManager.StartMonitoring(configService.GetDevices());
        Console.WriteLine("Device monitoring started.");
    }
    else
    {
        Console.WriteLine("Failed to load configuration.");
    }
}
else
{
    Console.WriteLine($"Configuration file not found at: {normalizedPath}");
    Console.WriteLine("Upload configuration via API: POST /api/DeviceConfig/upload");
}

// Graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    var scanManager = app.Services.GetRequiredService<DeviceScanManager>();
    await scanManager.StopMonitoringAsync();
    
    var driver = app.Services.GetRequiredService<IModbusDriver>();
    driver.Dispose();
});

app.Run();
