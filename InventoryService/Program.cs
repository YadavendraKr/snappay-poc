using InventoryService.CQRS;
using InventoryService.Infrastructure;
using InventoryService.Middleware;
using InventoryService.Services;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Optimization: Reduce payload size and enable server-side caching
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddOutputCache(options =>
{
    // Default policy: Cache for 10 seconds to ensure stock levels stay relatively fresh
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(10)));
});

// Shared Infrastructure for CQRS and Event-Driven Sync
builder.Services.AddSingleton<InventoryDbContext>();
builder.Services.AddSingleton<IMediator, ServiceMediator>();

// CQRS Handlers
builder.Services.AddSingleton<ICommandHandler<ReduceStockCommand>, ReduceStockCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<IncreaseStockCommand>, IncreaseStockCommandHandler>();
builder.Services.AddSingleton<ICommandHandler<CreateProductCommand>, CreateProductCommandHandler>();
builder.Services.AddSingleton<IQueryHandler<GetProductQuery, Product>, GetProductQueryHandler>();
builder.Services.AddSingleton<IQueryHandler<GetAllProductsQuery, List<Product>>, GetAllProductsQueryHandler>();
builder.Services.AddSingleton<IQueryHandler<GetProductStockQuery, int>, GetProductStockQueryHandler>();


builder.Services.AddScoped<IInventoryService, InventoryDomainService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseOutputCache();

// On-demand sync from blocked_amounts.txt instead of polling
app.UseInventoryUpdateMiddleware();

app.MapControllers();

app.Run();