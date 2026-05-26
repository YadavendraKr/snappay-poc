using CustomerService.Models;
using Shared.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Collections.Concurrent;

namespace CustomerService.Middleware;

public class WalletUpdateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WalletUpdateMiddleware> _logger;
    // POC Idempotency tracker: In production, this would be a table in the Customer database
    private static readonly ConcurrentDictionary<string, byte> _processedEvents = new();

    public WalletUpdateMiddleware(RequestDelegate next, ILogger<WalletUpdateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CustomerDbContext dbContext)
    {
        try
        {
            // Update wallet from blocked amounts on every API call
            await UpdateWalletsFromBlockedAmountsAsync(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wallets in middleware");
        }

        await _next(context);
    }

    private async Task UpdateWalletsFromBlockedAmountsAsync(CustomerDbContext dbContext)
    {
        var lines = await FileStoreUtility.ReadAllLinesAsync();
        if (!lines.Any()) return;

        // Group confirmed events by CustomerId to aggregate all amounts for reduction
        var confirmedGroups = lines
            .Select(line =>
            {
                var parts = line.Split('|');
                if (parts.Length < 4) return null;

                var status = parts[2]?.Trim() ?? string.Empty;

                // Strictly consolidate ONLY "Confirmed" events for wallet reduction; ignore "Pending"
                if (!status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                    return null;

                var customerIdPart = parts[0]?.Trim() ?? string.Empty;
                var amountPart = parts[1]?.Trim() ?? string.Empty;
                var timestamp = parts[3]?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(timestamp) && !_processedEvents.ContainsKey(timestamp) &&
                    int.TryParse(customerIdPart, out var customerId) && 
                    decimal.TryParse(amountPart, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    return new { CustomerId = customerId, Amount = amount, Timestamp = timestamp };
                }
                return null;
            })
            .Where(x => x != null)
            .GroupBy(x => x!.CustomerId);
        
        foreach (var group in confirmedGroups)
        {
            var customerId = group.Key;
            var totalDeduction = group.Sum(x => x!.Amount);
            
            var customer = dbContext.GetCustomerById(customerId);
            if (customer != null)
            {
                var oldBalance = customer.Wallet;
                customer.Wallet -= (int)totalDeduction;
                
                _logger.LogInformation(
                    $"SUMMED DEDUCTION: Adding all {group.Count()} confirmed amounts for Customer {customerId}. " +
                    $"Total: {totalDeduction}. Wallet: {oldBalance} -> {customer.Wallet}"
                );

                // Mark all events in this group as processed in-memory
                foreach (var ev in group)
                {
                    _processedEvents.TryAdd(ev!.Timestamp, 0);
                }
            }
            else
            {
                _logger.LogWarning($"Pending amounts found for Customer {customerId} but customer not found in DB.");
            }
        }
    }
}

public static class WalletUpdateMiddlewareExtensions
{
    public static IApplicationBuilder UseWalletUpdateMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WalletUpdateMiddleware>();
    }
}
