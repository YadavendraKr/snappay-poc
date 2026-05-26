using Shared.Infrastructure;
using CustomerService.Models;
using System.Collections.Concurrent;

namespace CustomerService.Services;

public class BlockedAmountEventProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockedAmountEventProcessorService> _logger;
    // Increased from 5 seconds to 10 seconds for better performance
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public BlockedAmountEventProcessorService(IServiceProvider serviceProvider, ILogger<BlockedAmountEventProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlockedAmountEventProcessor service started");
        
        // Initial delay to allow services to start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBlockedAmountEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing blocked amount events");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("BlockedAmountEventProcessor service stopped");
    }

    private async Task ProcessBlockedAmountEventsAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

            try
            {
                _logger.LogDebug("Reading blocked amount events from file");
                var lines = await FileStoreUtility.ReadAllLinesAsync();

                if (lines.Count == 0)
                {
                    _logger.LogDebug("No blocked amount events to process");
                    return;
                }

                _logger.LogInformation($"Processing {lines.Count} blocked amount events");

                // Process events in parallel for better performance
                var processingTasks = lines.Select(async line =>
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        var customerId = int.Parse(parts[0]);
                        var amount = decimal.Parse(parts[1]);
                        var status = parts[2];
                        var timestamp = parts[3];
                        await ProcessSingleEventAsync(customerId, amount, status, timestamp, dbContext);
                    }
                });

                await Task.WhenAll(processingTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessBlockedAmountEventsAsync");
            }
        }
    }

    private Task ProcessSingleEventAsync(int customerId, decimal blockedAmount, string status, string timestamp, CustomerDbContext dbContext)
    {
        // Wallet processing is now handled by WalletUpdateMiddleware to reduce latency and prevent race conditions.
        _logger.LogDebug($"Skipping background processing for customer {customerId} - handled by Middleware.");
        return Task.CompletedTask;
    }
}
