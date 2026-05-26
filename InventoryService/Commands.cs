using Shared.Models;

namespace InventoryService.CQRS;

// Commands
public interface ICommand { }

public class ReduceStockCommand : ICommand
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; } = "Order";
}

public class IncreaseStockCommand : ICommand
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; } = "Return";
}

public class CreateProductCommand : ICommand
{
    public string? ProductName { get; set; }
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
}

// Queries
public interface IQuery<out TResult> { }

public class GetProductQuery : IQuery<Product>
{
    public int ProductId { get; set; }
}

public class GetAllProductsQuery : IQuery<List<Product>> { }

public class GetProductStockQuery : IQuery<int>
{
    public int ProductId { get; set; }
}

public class GetInventoryTransactionsQuery : IQuery<List<InventoryTransaction>>
{
    public int ProductId { get; set; }
}

public class CommandResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }

    public static CommandResult CreateSuccess(string message = "Operation successful", object? data = null)
    {
        return new CommandResult { Success = true, Message = message, Data = data };
    }

    public static CommandResult CreateFailure(string message)
    {
        return new CommandResult { Success = false, Message = message };
    }
}