using ModbusPerfTest.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<DeviceConfigService>();
builder.Services.AddSingleton<ScanTaskQueue>();
builder.Services.AddSingleton<MetricCollector>();

// Register Modbus driver based on configuration
// Set "UseMockModbus" to true in appsettings.json to use mock driver
var useMockModbus = builder.Configuration.GetValue<bool>("UseMockModbus", false);
var mockMinLatency = builder.Configuration.GetValue<int>("MockModbus:MinLatencyMs", 20);
var mockMaxLatency = builder.Configuration.GetValue<int>("MockModbus:MaxLatencyMs", 120);

if (useMockModbus)
{
    Console.WriteLine($"Using MOCK Modbus driver (latency: {mockMinLatency}-{mockMaxLatency}ms)");
    builder.Services.AddSingleton<IModbusDriver>(sp => new MockModbusDriver(mockMinLatency, mockMaxLatency));
}
else
{
    Console.WriteLine("Using REAL Modbus driver (NModbus TCP)");
    builder.Services.AddSingleton<IModbusDriver, ModbusDriver>();
}

builder.Services.AddSingleton<ClockDriftService>();
builder.Services.AddSingleton<DataQualityService>();
builder.Services.AddSingleton<DeviceScanManager>();

var app = builder.Build();

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
