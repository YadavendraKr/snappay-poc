using Shared.Infrastructure;
using OrderService.Models;

namespace OrderService.Services;

public class BlockedAmountCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockedAmountCleanupService> _logger;
    // Increased from 3 seconds to 15 seconds for better performance
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(15);

    public BlockedAmountCleanupService(IServiceProvider serviceProvider, ILogger<BlockedAmountCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlockedAmountCleanupService started");
        
        // Initial delay to allow services to start
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupConfirmedBlockedAmountsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BlockedAmountCleanupService");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("BlockedAmountCleanupService stopped");
    }

    private async Task CleanupConfirmedBlockedAmountsAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var subOrderDbContext = scope.ServiceProvider.GetRequiredService<SubOrderDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlockedAmountCleanupService>>();

            try
            {
                // Get all sub-orders to check which customers have pending blocked amounts
                var blockedAmounts = subOrderDbContext.GetAllBlockedAmounts();
                
                if (blockedAmounts.Count == 0)
                {
                    logger.LogDebug("No blocked amounts to clean up");
                    return;
                }

                logger.LogInformation($"Checking {blockedAmounts.Count} blocked amounts for cleanup");

                // Process cleanup in parallel for better performance
                var cleanupTasks = blockedAmounts.Keys.Select(async customerId =>
                {
                    try
                    {
                        // Cleanup logic is now synchronized with middleware deletion
                        _logger.LogDebug($"Cleanup check for customer {customerId} deferred to middleware lifecycle.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error cleaning up blocked amount for customer {customerId}");
                    }
                });

                await Task.WhenAll(cleanupTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CleanupConfirmedBlockedAmountsAsync");
            }
        }
    }
}
