using Shared.Models;
using Microsoft.Extensions.Logging;
using InventoryService.CQRS;

namespace InventoryService.Services;

public interface IInventoryService
{
    Task<int> GetStockAsync(int productId);
    Task<Product?> GetProductAsync(int productId);
    Task<List<Product>> GetAllProductsAsync();
    Task<CommandResult> ReduceStockAsync(int productId, int quantity, string reason = "Order");
    Task<CommandResult> IncreaseStockAsync(int productId, int quantity, string reason = "Return");
    Task<CommandResult> CreateProductAsync(string productName, int stock, decimal price);
}

public class InventoryDomainService : IInventoryService
{
    private readonly IMediator _mediator;
    private readonly ILogger<InventoryDomainService> _logger;

    public InventoryDomainService(IMediator mediator, ILogger<InventoryDomainService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<int> GetStockAsync(int productId)
    {
        return await _mediator.QueryAsync<GetProductStockQuery, int>(new GetProductStockQuery { ProductId = productId });
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        return await _mediator.QueryAsync<GetProductQuery, Product>(new GetProductQuery { ProductId = productId });
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        var products = await _mediator.QueryAsync<GetAllProductsQuery, List<Product>>(new GetAllProductsQuery());
        return products ?? new List<Product>();
    }

    public async Task<CommandResult> ReduceStockAsync(int productId, int quantity, string reason = "Order")
    {
        return await _mediator.SendAsync(new ReduceStockCommand { ProductId = productId, Quantity = quantity, Reason = reason });
    }

    public async Task<CommandResult> IncreaseStockAsync(int productId, int quantity, string reason = "Return")
    {
        return await _mediator.SendAsync(new IncreaseStockCommand { ProductId = productId, Quantity = quantity, Reason = reason });
    }

    public async Task<CommandResult> CreateProductAsync(string productName, int stock, decimal price)
    {
        var command = new CreateProductCommand { ProductName = productName, StockQuantity = stock, Price = price };
        return await _mediator.SendAsync(command);
    }
}