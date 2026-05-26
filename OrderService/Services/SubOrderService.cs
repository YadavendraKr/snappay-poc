using Shared.Infrastructure;
using Shared.Models;
using OrderService.Models;

namespace OrderService.Services;

public interface ISubOrderService
{
    Task<SubOrder> AddSubOrderAsync(int customerId, decimal amount, decimal walletAmount);
    Task<List<SubOrder>> GetSubOrdersByCustomerIdAsync(int customerId);
    Task<decimal> GetBlockedAmountAsync(int customerId);
    Task<bool> CanPlaceOrderAsync(int customerId, decimal walletAmount);
    Task ClearBlockedAmountAsync(int customerId);
}

public class SubOrderService : ISubOrderService
{
    private readonly SubOrderDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SubOrderService> _logger;
    private const string BlockedAmountCacheKeyPrefix = "blocked_amount:";
    private const int CacheDurationMinutes = 5;


    public SubOrderService(
        SubOrderDbContext dbContext,
        ICacheService cacheService,
        ILogger<SubOrderService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<SubOrder> AddSubOrderAsync(int customerId, decimal amount, decimal walletAmount)
    {
        _logger.LogInformation($"Adding sub-order for customer {customerId} with amount {amount}");

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        var subOrder = new SubOrder
        {
            CustomerId = customerId,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        };

        // Get current blocked amount
        var currentBlockedAmount = _dbContext.GetBlockedAmount(customerId);
        var newBlockedAmount = currentBlockedAmount + amount;

        // Check if adding this sub-order would exceed wallet
        if (newBlockedAmount > walletAmount)
        {
            var message = $"Sub-order amount {amount} cannot be added. Current blocked amount is {currentBlockedAmount}, " +
                         $"and adding this sub-order would result in {newBlockedAmount}, which exceeds wallet balance of {walletAmount}. " +
                         $"Please reduce the sub-order amount or add funds to your wallet.";
            
            _logger.LogWarning(message);
            throw new ArgumentException(message);
        }

        // Safe to add to blocked amount
        var createdSubOrder = _dbContext.AddSubOrder(subOrder);

        // Write event for EDA
        await FileStoreUtility.WriteEventAsync(customerId, newBlockedAmount, "Pending");

        // Invalidate cache
        var cacheKey = $"{BlockedAmountCacheKeyPrefix}{customerId}";
        try
        {
            await _cacheService.RemoveAsync(cacheKey);
        }
        catch
        { }

        _logger.LogInformation($"Sub-order added. Total blocked amount for customer {customerId}: {newBlockedAmount}");

        return createdSubOrder;
    }

    public async Task<List<SubOrder>> GetSubOrdersByCustomerIdAsync(int customerId)
    {
        _logger.LogInformation($"Fetching sub-orders for customer {customerId}");

        var subOrders = _dbContext.GetSubOrdersByCustomerId(customerId);
        return subOrders;
    }

    public async Task<decimal> GetBlockedAmountAsync(int customerId)
    {

        _logger.LogInformation($"Fetching blocked amount for customer {customerId}");

        var cacheKey = $"{BlockedAmountCacheKeyPrefix}{customerId}";
        decimal blockedAmount = 0;

        try
        {
            // ✅ 1. Check disk directly (FileStoreUtility)
            var fileAmount = await FileStoreUtility.GetBlockedAmountAsync(customerId);

            if (fileAmount > 0)
            {
                _logger.LogInformation($"Blocked amount for customer {customerId} retrieved from file: {fileAmount}");
                await _cacheService.SetAsync(cacheKey, fileAmount, TimeSpan.FromMinutes(CacheDurationMinutes));
                blockedAmount = fileAmount;
            }
            else
            {
                blockedAmount = _dbContext.GetBlockedAmount(customerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching blocked amount for {customerId}");
            blockedAmount = _dbContext.GetBlockedAmount(customerId);
        }
        return blockedAmount;
    }

    public async Task<bool> CanPlaceOrderAsync(int customerId, decimal walletAmount)
    {
        var blockedAmount = await GetBlockedAmountAsync(customerId);
        var canPlace = walletAmount >= blockedAmount;

        _logger.LogInformation($"Customer {customerId} - Wallet: {walletAmount}, BlockedAmount: {blockedAmount}, Can Place Order: {canPlace}");

        return canPlace;
    }

    public async Task ClearBlockedAmountAsync(int customerId)
    {
        _logger.LogInformation($"Clearing blocked amount for customer {customerId}");

        _dbContext.ClearBlockedAmount(customerId);

        // Invalidate cache
        var cacheKey = $"{BlockedAmountCacheKeyPrefix}{customerId}";
        await _cacheService.RemoveAsync(cacheKey);
    }
}