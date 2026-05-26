using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using InventoryService.CQRS;
using InventoryService.Infrastructure;
using Shared.Infrastructure;

namespace InventoryService.Middleware;

public class InventoryUpdateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InventoryUpdateMiddleware> _logger;

    public InventoryUpdateMiddleware(RequestDelegate next, ILogger<InventoryUpdateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMediator mediator, InventoryDbContext dbContext)
    {
        try
        {
            await UpdateInventoryFromEventsAsync(mediator, dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory in middleware");
        }
        await _next(context);
    }

    private async Task UpdateInventoryFromEventsAsync(IMediator mediator, InventoryDbContext dbContext)
    {
        var lines = await FileStoreUtility.ReadAllLinesAsync();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;

            var status = parts[2].Trim();
            var timestamp = parts[3].Trim();

            if (status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                var alreadyProcessed = dbContext.GetTransactions(1).Any(t => t.Reason != null && t.Reason.Contains(timestamp));
                if (!alreadyProcessed)
                {
                    var command = new ReduceStockCommand 
                    { 
                        ProductId = 1, 
                        Quantity = 1, 
                        Reason = $"Auto-sync order {timestamp}" 
                    };
                    var result = await mediator.SendAsync(command);
                    if (result.Success)
                    {
                        _logger.LogInformation($"Stock reduced for order {timestamp}.");
                    }
                }
            }
        }
    }
}

public static class InventoryUpdateMiddlewareExtensions
{
    public static IApplicationBuilder UseInventoryUpdateMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<InventoryUpdateMiddleware>();
    }
}