using Microsoft.Extensions.Logging;
using InventoryService.CQRS;
using Shared.Models;

namespace InventoryService.Infrastructure;

public class InventoryDbContext
{
    private static List<Product> _products = new()
    {
        new Product { Id = 1, ProductName = "Laptop", StockQuantity = 50, Price = 999.99m, LastUpdated = DateTime.UtcNow },
        new Product { Id = 2, ProductName = "Mouse", StockQuantity = 200, Price = 29.99m, LastUpdated = DateTime.UtcNow },
        new Product { Id = 3, ProductName = "Keyboard", StockQuantity = 150, Price = 79.99m, LastUpdated = DateTime.UtcNow }
    };

    private static List<InventoryTransaction> _transactions = new();
    private static int _nextProductId = 4;
    private static int _nextTransactionId = 1;
    private static readonly object _lockObject = new object();

    public Product? GetProduct(int productId)
    {
        lock (_lockObject) return _products.FirstOrDefault(p => p.Id == productId);
    }

    public List<Product> GetAllProducts()
    {
        lock (_lockObject) return new List<Product>(_products);
    }

    public bool UpdateProductStock(int productId, int newStock)
    {
        lock (_lockObject)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return false;
            product.StockQuantity = newStock;
            product.LastUpdated = DateTime.UtcNow;
            return true;
        }
    }

    public Product? CreateProduct(string productName, int stock, decimal price)
    {
        lock (_lockObject)
        {
            var product = new Product { Id = _nextProductId++, ProductName = productName, StockQuantity = stock, Price = price, LastUpdated = DateTime.UtcNow };
            _products.Add(product);
            return product;
        }
    }

    public void AddTransaction(int productId, int quantityChanged, string reason)
    {
        lock (_lockObject)
        {
            _transactions.Add(new InventoryTransaction { Id = _nextTransactionId++, ProductId = productId, QuantityChanged = quantityChanged, Reason = reason, TransactionDate = DateTime.UtcNow });
        }
    }

    public List<InventoryTransaction> GetTransactions(int productId)
    {
        lock (_lockObject) return _transactions.Where(t => t.ProductId == productId).ToList();
    }
}

public class ReduceStockCommandHandler : ICommandHandler<ReduceStockCommand>
{
    private readonly InventoryDbContext _dbContext;
    private readonly ILogger<ReduceStockCommandHandler> _logger;

    public ReduceStockCommandHandler(InventoryDbContext dbContext, ILogger<ReduceStockCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CommandResult> HandleAsync(ReduceStockCommand command)
    {
        var product = _dbContext.GetProduct(command.ProductId);
        if (product == null) return CommandResult.CreateFailure($"Product {command.ProductId} not found");
        if (product.StockQuantity < command.Quantity) return CommandResult.CreateFailure("Insufficient stock.");

        var newStock = product.StockQuantity - command.Quantity;
        if (_dbContext.UpdateProductStock(command.ProductId, newStock))
        {
            _dbContext.AddTransaction(command.ProductId, -command.Quantity, command.Reason ?? "Order");
            return CommandResult.CreateSuccess($"Stock reduced by {command.Quantity}");
        }
        return CommandResult.CreateFailure("Failed to update stock");
    }
}

public class IncreaseStockCommandHandler : ICommandHandler<IncreaseStockCommand>
{
    private readonly InventoryDbContext _dbContext;

    public IncreaseStockCommandHandler(InventoryDbContext dbContext) => _dbContext = dbContext;

    public async Task<CommandResult> HandleAsync(IncreaseStockCommand command)
    {
        var product = _dbContext.GetProduct(command.ProductId);
        if (product == null) return CommandResult.CreateFailure("Product not found");
        var newStock = product.StockQuantity + command.Quantity;
        _dbContext.UpdateProductStock(command.ProductId, newStock);
        _dbContext.AddTransaction(command.ProductId, command.Quantity, command.Reason ?? "Return");
        return CommandResult.CreateSuccess($"Stock increased by {command.Quantity}");
    }
}

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand>
{
    private readonly InventoryDbContext _dbContext;

    public CreateProductCommandHandler(InventoryDbContext dbContext) => _dbContext = dbContext;

    public async Task<CommandResult> HandleAsync(CreateProductCommand command)
    {
        var product = _dbContext.CreateProduct(command.ProductName!, command.StockQuantity, command.Price);
        return product != null ? CommandResult.CreateSuccess("Created", product) : CommandResult.CreateFailure("Failed");
    }
}

public class GetProductQueryHandler : IQueryHandler<GetProductQuery, Product>
{
    private readonly InventoryDbContext _dbContext;
    public GetProductQueryHandler(InventoryDbContext dbContext) => _dbContext = dbContext;
    public async Task<Product?> HandleAsync(GetProductQuery query) => _dbContext.GetProduct(query.ProductId);
}

public class GetAllProductsQueryHandler : IQueryHandler<GetAllProductsQuery, List<Product>>
{
    private readonly InventoryDbContext _dbContext;
    public GetAllProductsQueryHandler(InventoryDbContext dbContext) => _dbContext = dbContext;
    public async Task<List<Product>?> HandleAsync(GetAllProductsQuery query) => _dbContext.GetAllProducts();
}

public class GetProductStockQueryHandler : IQueryHandler<GetProductStockQuery, int>
{
    private readonly InventoryDbContext _dbContext;
    public GetProductStockQueryHandler(InventoryDbContext dbContext) => _dbContext = dbContext;
    public async Task<int> HandleAsync(GetProductStockQuery query) => _dbContext.GetProduct(query.ProductId)?.StockQuantity ?? 0;
}

public class GetInventoryTransactionsQueryHandler : IQueryHandler<GetInventoryTransactionsQuery, List<InventoryTransaction>>
{
    private readonly InventoryDbContext _dbContext;
    public GetInventoryTransactionsQueryHandler(InventoryDbContext dbContext) => _dbContext = dbContext;
    public async Task<List<InventoryTransaction>?> HandleAsync(GetInventoryTransactionsQuery query) => _dbContext.GetTransactions(query.ProductId);
}