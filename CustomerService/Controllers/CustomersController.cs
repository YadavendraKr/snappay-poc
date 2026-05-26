using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using CustomerService.Services;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAllCustomers()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(customers);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetCustomersPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "Page >= 1 and PageSize 1-100 required" });

        var result = await _customerService.GetCustomersPagedAsync(page, pageSize);
        
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(new
        {
            items = result.Items,
            total = result.Total,
            page,
            pageSize,
            totalPages = (result.Total + pageSize - 1) / pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomerById(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid customer ID" });
        
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound(new { message = "Customer not found" });

        Response.Headers.CacheControl = "public, max-age=600";
        return Ok(customer);
    }

    [HttpGet("{id}/with-user-details")]
    public async Task<ActionResult<CustomerWithUserDetailsDto>> GetCustomerWithUserDetails(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid customer ID" });
        
        var customerWithUserDetails = await _customerService.GetCustomerWithUserDetailsAsync(id);
        if (customerWithUserDetails == null)
            return NotFound(new { message = "Customer not found" });

        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(customerWithUserDetails);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
            return BadRequest(new { message = "Name and Email required" });

        var createdCustomer = await _customerService.CreateCustomerAsync(customer);
        return CreatedAtAction(nameof(GetCustomerById), new { id = createdCustomer.Id }, createdCustomer);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid customer ID" });
        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
            return BadRequest(new { message = "Name and Email required" });

        var result = await _customerService.UpdateCustomerAsync(id, customer);
        if (!result)
            return NotFound(new { message = "Customer not found" });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCustomer(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid customer ID" });
        
        var result = await _customerService.DeleteCustomerAsync(id);
        if (!result)
            return NotFound(new { message = "Customer not found" });

        return NoContent();
    }
}
