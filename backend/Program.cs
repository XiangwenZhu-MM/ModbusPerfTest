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

// Register RuntimeConfigService
builder.Services.AddSingleton<RuntimeConfigService>();

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
// Register IDataPointRepository based on DataPointStorage config
var dataPointStorageConfig = builder.Configuration.GetSection("DataPointStorage");
var backendType = dataPointStorageConfig.GetValue<string>("Backend", "SQLite");
if (backendType.Equals("InfluxDB", StringComparison.OrdinalIgnoreCase))
{
    var influxConfig = dataPointStorageConfig.GetSection("InfluxDB");
    var url = influxConfig.GetValue<string>("Url", "http://localhost:8086");
    var token = influxConfig.GetValue<string>("Token", "");
    var bucket = influxConfig.GetValue<string>("Bucket", "modbus");
    var org = influxConfig.GetValue<string>("Org", "modbus-org");
    builder.Services.AddSingleton<IDataPointRepository>(sp => new InfluxDataPointRepository(url, token, bucket, org));
    Console.WriteLine($"Using InfluxDB backend for data points: {url}, bucket={bucket}, org={org}");
}
else
{
    builder.Services.AddSingleton<IDataPointRepository, DataPointRepository>();
    Console.WriteLine("Using SQLite backend for data points");
}

// Register DataPointBuffer with IDataPointRepository
builder.Services.AddSingleton<DataPointBuffer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataPointBuffer>());
builder.Services.AddSingleton<DeviceConfigService>();
builder.Services.AddSingleton<ScanTaskQueue>();
builder.Services.AddSingleton<MetricCollector>();

// Register Modbus exception logger
builder.Services.AddSingleton<ModbusExceptionLogger>();

// Register Modbus driver based on configuration
// Set "UseAsyncRead" to true to use async Modbus read operations (recommended)
// Set "UseAsyncNModbus" to true to use NModbusAsync library (wolf8196 fork) instead of standard NModbus
var useAsyncRead = builder.Configuration.GetValue<bool>("UseAsyncRead", true);
var useAsyncNModbus = builder.Configuration.GetValue<bool>("UseAsyncNModbus", false);

if (useAsyncRead)
{
    if (useAsyncNModbus)
    {
        Console.WriteLine("Using ASYNC read with NModbusAsync library (NModbusAsyncDriver)");
        builder.Services.AddSingleton<IModbusDriver, NModbusAsyncDriver>();
    }
    else
    {
        Console.WriteLine("Using ASYNC read with NModbus library (HighPerformanceModbusDriver)");
        builder.Services.AddSingleton<IModbusDriver, HighPerformanceModbusDriver>();
    }
}
else
{
    if (useAsyncNModbus)
    {
        Console.WriteLine("WARNING: NModbusAsync library is only supported with async reads. Using NModbus with sync reads instead.");
    }
    
    Console.WriteLine("Using SYNC read with NModbus library (ModbusDriver)");
    builder.Services.AddSingleton<IModbusDriver, ModbusDriver>();
}

builder.Services.AddSingleton<ClockDriftService>();
builder.Services.AddSingleton<DataQualityService>();
builder.Services.AddSingleton<DeviceScanManager>();

// Initialize RuntimeConfigService with current configuration
var runtimeConfigService = new RuntimeConfigService();
runtimeConfigService.Initialize(new RuntimeConfig
{
    UseAsyncRead = useAsyncRead,
    UseAsyncNModbus = useAsyncNModbus,
    AllowConcurrentFrameReads = builder.Configuration.GetValue<bool>("AllowConcurrentFrameReads", false),
    DataStorageBackend = builder.Configuration.GetSection("DataPointStorage").GetValue<string>("Backend", "SQLite") ?? "SQLite",
    MinWorkerThreads = minWorkerThreads
});
builder.Services.AddSingleton(runtimeConfigService);

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

// Register ResourceMonitorService
builder.Services.AddSingleton<ResourceMonitorService>();
builder.Services.AddHostedService<ResourceMonitorService>(sp => sp.GetRequiredService<ResourceMonitorService>());

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

// Get config file name from appsettings.json, defaulting to device-config.json
var configFileName = app.Configuration.GetValue<string>("DeviceConfigPath", "device-config.json");
var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", configFileName);
var normalizedPath = Path.GetFullPath(configPath);

if (File.Exists(normalizedPath))
{
    Console.WriteLine($"Loading device configuration from: {normalizedPath}");
    var loaded = await configService.LoadFromFileAsync(normalizedPath);
    if (loaded)
    {
        Console.WriteLine($"Configuration loaded successfully. Devices: {configService.GetDevices().Count}");
        Console.WriteLine("Device scanning is NOT started automatically. Use Start button in dashboard or POST /api/ScanControl/start");
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
