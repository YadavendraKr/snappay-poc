using Shared.Infrastructure;
using Shared.Models;
using OrderService.Models;

namespace OrderService.Services;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(int id);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
    Task<Order> CreateOrderAsync(Order order);
    Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status);
    Task<bool> DeleteOrderAsync(int id);
}

public class OrderServiceImpl : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICustomerServiceClient _customerServiceClient;
    private readonly ISubOrderService _subOrderService;
    private readonly ILogger<OrderServiceImpl> _logger;
    private const string CacheKeyPrefix = "order:";
    private const string AllOrdersCacheKey = "all:orders";
    private const string CustomerOrdersCacheKeyPrefix = "customer:orders:";
    private const int CacheDurationMinutes = 15;

    public OrderServiceImpl(
        OrderDbContext dbContext,
        ICacheService cacheService,
        ICustomerServiceClient customerServiceClient,
        ISubOrderService subOrderService,
        ILogger<OrderServiceImpl> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _customerServiceClient = customerServiceClient;
        _subOrderService = subOrderService;
        _logger = logger;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        var cachedOrders = await _cacheService.GetAsync<List<Order>>(AllOrdersCacheKey);
        if (cachedOrders != null)
            return cachedOrders;

        var orders = _dbContext.GetAllOrders();
        _ = _cacheService.SetAsync(AllOrdersCacheKey, orders, TimeSpan.FromMinutes(CacheDurationMinutes));
        return orders;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cachedOrder = await _cacheService.GetAsync<Order>(cacheKey);
        if (cachedOrder != null)
            return cachedOrder;

        var order = _dbContext.GetOrderById(id);
        if (order == null)
            return null;

        _ = _cacheService.SetAsync(cacheKey, order, TimeSpan.FromMinutes(CacheDurationMinutes));
        return order;
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
    {
        var cacheKey = $"{CustomerOrdersCacheKeyPrefix}{customerId}";
        var cachedOrders = await _cacheService.GetAsync<List<Order>>(cacheKey);
        if (cachedOrders != null)
            return cachedOrders;

        var orders = _dbContext.GetOrdersByCustomerId(customerId);
        _ = _cacheService.SetAsync(cacheKey, orders, TimeSpan.FromMinutes(CacheDurationMinutes));
        return orders;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        var customerTask = _customerServiceClient.GetCustomerAsync(order.CustomerId);
        var blockedAmountTask = _subOrderService.GetBlockedAmountAsync(order.CustomerId);
        await Task.WhenAll(customerTask, blockedAmountTask);
        
        var customer = customerTask.Result;
        if (customer == null)
            throw new Exception($"Customer {order.CustomerId} not found");

        var blockedAmount = blockedAmountTask.Result;
        
        if (order.TotalAmount < blockedAmount)
            throw new Exception($"Order amount {order.TotalAmount} must be >= blocked amount {blockedAmount}");

        if (customer.Wallet < order.TotalAmount)
            throw new Exception($"Insufficient funds. Wallet: {customer.Wallet}, Order Amount: {order.TotalAmount}");

        order.BlockedAmount = blockedAmount;
        order.Status = OrderStatus.Confirmed;

        var createdOrder = _dbContext.CreateOrder(order);

        _ = Task.Run(async () =>
        {
            try
            {
                await FileStoreUtility.WriteEventAsync(order.CustomerId, order.TotalAmount, "Confirmed");
                await _subOrderService.ClearBlockedAmountAsync(order.CustomerId);
                await _cacheService.RemoveAsync(AllOrdersCacheKey);
                await _cacheService.RemoveAsync($"{CustomerOrdersCacheKeyPrefix}{order.CustomerId}");
            }
            catch { }
        });

        return createdOrder;
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        var order = _dbContext.GetOrderById(id);
        if (order == null)
            return false;

        if (order.Status == OrderStatus.Confirmed)
            return false;

        var oldStatus = order.Status;
        var result = _dbContext.UpdateOrderStatus(id, status);
        if (!result) 
            return false;

        if (status == OrderStatus.Confirmed && oldStatus != OrderStatus.Confirmed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await FileStoreUtility.WriteEventAsync(order.CustomerId, order.TotalAmount, "Confirmed");
                    await _subOrderService.ClearBlockedAmountAsync(order.CustomerId);
                }
                catch { }
            });
        }

        var cacheKey = $"{CacheKeyPrefix}{id}";
        _ = Task.WhenAll(
            _cacheService.RemoveAsync(cacheKey),
            _cacheService.RemoveAsync(AllOrdersCacheKey)
        );

        return true;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = _dbContext.GetOrderById(id);
        if (order == null) 
            return false;

        if (order.Status == OrderStatus.Confirmed)
            return false;

        var result = _dbContext.DeleteOrder(id);
        if (!result) 
            return false;

        var cacheKey = $"{CacheKeyPrefix}{id}";
        _ = Task.WhenAll(
            _cacheService.RemoveAsync(cacheKey),
            _cacheService.RemoveAsync(AllOrdersCacheKey),
            _cacheService.RemoveAsync($"{CustomerOrdersCacheKeyPrefix}{order.CustomerId}")
        );

        return true;
    }
}
