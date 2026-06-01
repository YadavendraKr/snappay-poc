using Microsoft.AspNetCore.ResponseCompression;
using StackExchange.Redis;
using Shared.Infrastructure;
using Shared.Models;
using OrderService.Models;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add response compression - use Fastest level for low latency
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Add optimized Redis connection
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
sp =>
{
    var options = ConfigurationOptions.Parse(redisConnection);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 1000;
    options.SyncTimeout = 1000;
    options.KeepAlive = 60;
    options.DefaultDatabase = 0;
    return ConnectionMultiplexer.Connect(options);
});

// Add cache service
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Add database contexts
builder.Services.AddSingleton<OrderDbContext>();
builder.Services.AddSingleton<SubOrderDbContext>();

// Add business services
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();
builder.Services.AddScoped<ISubOrderService, SubOrderService>();

// Add optimized HTTP client for inter-service communication with retry logic
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMilliseconds(3000); // Increased to allow for retries
        client.DefaultRequestHeaders.ConnectionClose = false;
    });

// Add CORS for inter-service communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use response compression middleware early
app.UseResponseCompression();

// Dapr request logging middleware removed — sidecar/observability handles this

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
