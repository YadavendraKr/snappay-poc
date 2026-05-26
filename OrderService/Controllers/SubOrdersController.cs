using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubOrdersController : ControllerBase
{
    private readonly ISubOrderService _subOrderService;
    private readonly IOrderService _orderService;
    private readonly ICustomerServiceClient _customerServiceClient;
    private readonly ILogger<SubOrdersController> _logger;

    public SubOrdersController(
        ISubOrderService subOrderService,
        IOrderService orderService,
        ICustomerServiceClient customerServiceClient,
        ILogger<SubOrdersController> logger)
    {
        _subOrderService = subOrderService;
        _orderService = orderService;
        _customerServiceClient = customerServiceClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<SubOrder>> AddSubOrder([FromBody] AddSubOrderRequest? request)
    {
        if (request == null || request.CustomerId <= 0 || request.Amount <= 0)
            return BadRequest(new { message = "Valid CustomerId and Amount (> 0) are required" });

        try
        {
            // Validate customer exists
            var customer = await _customerServiceClient.GetCustomerAsync(request.CustomerId);
            if (customer == null)
                return BadRequest(new { message = $"Customer {request.CustomerId} not found" });

            var subOrder = await _subOrderService.AddSubOrderAsync(request.CustomerId, request.Amount, customer.Wallet);
            
            var blockedAmount = await _subOrderService.GetBlockedAmountAsync(request.CustomerId);
            var canPlaceOrder = await _subOrderService.CanPlaceOrderAsync(request.CustomerId, customer.Wallet);

            return Ok(new
            {
                subOrder,
                blockedAmount,
                walletAmount = customer.Wallet,
                canPlaceOrder,
                message = canPlaceOrder 
                    ? "Blocked amount consolidated. You can now place an order." 
                    : "Sub-order added. Blocked amount is still accumulating."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding sub-order for customer {request.CustomerId}");
            return StatusCode(500, new { message = "An error occurred while processing the request" });
        }
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult> GetSubOrdersByCustomerId(int customerId)
    {
        var subOrders = await _subOrderService.GetSubOrdersByCustomerIdAsync(customerId);
        var blockedAmount = await _subOrderService.GetBlockedAmountAsync(customerId);
        
        var customer = await _customerServiceClient.GetCustomerAsync(customerId);
        var canPlaceOrder = customer != null && await _subOrderService.CanPlaceOrderAsync(customerId, customer.Wallet);

        Response.Headers.CacheControl = "public, max-age=60"; // 1 minute
        return Ok(new
        {
            customerId,
            subOrders,
            blockedAmount,
            walletAmount = customer?.Wallet ?? 0,
            canPlaceOrder,
            subOrderCount = subOrders.Count
        });
    }

    [HttpGet("blocked-amount/{customerId}")]
    public async Task<ActionResult> GetBlockedAmount(int customerId)
    {
        var blockedAmount = await _subOrderService.GetBlockedAmountAsync(customerId);
        var customer = await _customerServiceClient.GetCustomerAsync(customerId);
        
        if (customer == null)
            return NotFound(new { message = $"Customer {customerId} not found" });

        var canPlaceOrder = await _subOrderService.CanPlaceOrderAsync(customerId, customer.Wallet);

        Response.Headers.CacheControl = "public, max-age=60"; // 1 minute

        return Ok(new
        {
            customerId,
            blockedAmount,
            walletAmount = customer.Wallet,
            canPlaceOrder,
            deficit = Math.Max(0, blockedAmount - customer.Wallet)
        });
    }
}

public class AddSubOrderRequest
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
}
