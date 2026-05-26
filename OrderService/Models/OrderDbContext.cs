using Shared.Models;

namespace OrderService.Models;

public class OrderDbContext
{
    private static List<Order> _orders = new()
    {
        new Order
        {
            Id = 1,
            CustomerId = 1,
            TotalAmount = 299.99m,
            Status = OrderStatus.Delivered,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductName = "Laptop", Quantity = 1, UnitPrice = 299.99m }
            }
        },
        new Order
        {
            Id = 2,
            CustomerId = 2,
            TotalAmount = 150.00m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Items = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductName = "Mouse", Quantity = 2, UnitPrice = 25.00m },
                new OrderItem { Id = 2, ProductName = "Keyboard", Quantity = 1, UnitPrice = 100.00m }
            }
        }
    };

    private static int _nextId = 3;

    public List<Order> GetAllOrders() => _orders;

    public List<Order> GetOrdersByCustomerId(int customerId) => _orders.Where(o => o.CustomerId == customerId).ToList();

    public Order? GetOrderById(int id) => _orders.FirstOrDefault(o => o.Id == id);

    public Order CreateOrder(Order order)
    {
        order.Id = _nextId++;
        order.CreatedAt = DateTime.UtcNow;
        _orders.Add(order);
        return order;
    }

    public bool UpdateOrderStatus(int id, OrderStatus status)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return false;

        order.Status = status;
        return true;
    }

    public bool DeleteOrder(int id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return false;

        _orders.Remove(order);
        return true;
    }
}
