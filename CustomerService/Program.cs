using CustomerService.Models;
using Shared.Infrastructure;
using CustomerService.Services;
using CustomerService.Middleware;
using StackExchange.Redis;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services
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

// Register database context
builder.Services.AddSingleton<CustomerDbContext>();

// Add optimized Redis connection
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = ConfigurationOptions.Parse(redisConnection);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 1000;
    options.SyncTimeout = 1000;
    options.KeepAlive = 60;
    options.DefaultDatabase = 0;
    return ConnectionMultiplexer.Connect(options);
});

// Register cache service (Redis)
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Register gRPC client for UserService with optimized channel options
builder.Services.AddGrpcClient<global::UserService.Protos.UserService.UserServiceClient>(o =>
{
    var url = builder.Configuration["Services:UserServiceUrl"] ?? "http://localhost:5300";
    o.Address = new Uri(url);
}).ConfigureChannel(options =>
{
    options.HttpHandler = new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
        MaxConnectionsPerServer = 10,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip
    };
});

// Register business service
builder.Services.AddScoped<ICustomerService, CustomerService.Services.CustomerService>();

var app = builder.Build();

// Use response compression middleware early
app.UseResponseCompression();

// Configure middleware
app.UseMiddleware<WalletUpdateMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

