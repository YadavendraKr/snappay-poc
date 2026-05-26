using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid order ID" });
        
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found" });

        Response.Headers.CacheControl = "public, max-age=600";
        return Ok(order);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomerId(int customerId)
    {
        if (customerId <= 0)
            return BadRequest(new { message = "Invalid customer ID" });
        
        var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        if (order == null || order.CustomerId <= 0 || order.Items == null || order.Items.Count == 0 || order.TotalAmount <= 0)
            return BadRequest(new { message = "Valid CustomerId, Items, and TotalAmount required" });

        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(order);
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (request?.Status == null)
            return BadRequest(new { message = "Status is required" });

        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status.Value);
        if (!result)
            return NotFound(new { message = "Order not found" });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrder(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid order ID" });
        
        var result = await _orderService.DeleteOrderAsync(id);
        if (!result)
            return NotFound(new { message = "Order not found" });

        return NoContent();
    }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus? Status { get; set; }
}
