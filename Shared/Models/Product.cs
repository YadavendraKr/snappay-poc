namespace Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string? ProductName { get; set; }
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityChanged { get; set; }
    public string? Reason { get; set; } // "Order", "Return", "Adjustment"
    public DateTime TransactionDate { get; set; }
}
