using Shared.Infrastructure;
using Shared.Models;
using CustomerService.Models;

namespace CustomerService.Services;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<(List<Customer> Items, int Total)> GetCustomersPagedAsync(int pageNumber = 1, int pageSize = 10);
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<bool> UpdateCustomerAsync(int id, Customer customer);
    Task<bool> DeleteCustomerAsync(int id);
    Task<CustomerWithUserDetailsDto?> GetCustomerWithUserDetailsAsync(int customerId);
}

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly global::UserService.Protos.UserService.UserServiceClient _userServiceClient;
    private readonly ILogger<CustomerService> _logger;
    private const string CacheKeyPrefix = "customer:";
    private const string AllCustomersCacheKey = "all:customers";
    private const string PagedCustomersCacheKeyPrefix = "customers:page:";
    private const int CacheDurationMinutes = 15;

    public CustomerService(CustomerDbContext dbContext, ICacheService cacheService, global::UserService.Protos.UserService.UserServiceClient userServiceClient, ILogger<CustomerService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        var cachedCustomers = await _cacheService.GetAsync<List<Customer>>(AllCustomersCacheKey);
        if (cachedCustomers != null)
            return cachedCustomers;

        var customers = _dbContext.GetAllCustomers();
        // Fire and forget cache write
        _ = _cacheService.SetAsync(AllCustomersCacheKey, customers, TimeSpan.FromMinutes(CacheDurationMinutes));
        
        return customers;
    }

    public async Task<(List<Customer> Items, int Total)> GetCustomersPagedAsync(int pageNumber = 1, int pageSize = 10)
    {
        var cacheKey = $"{PagedCustomersCacheKeyPrefix}{pageNumber}:{pageSize}";
        var cached = await _cacheService.GetAsync<(List<Customer> Items, int Total)>(cacheKey);
        if (cached != default)
            return cached;

        var result = _dbContext.GetCustomersPaged(pageNumber, pageSize);
        // Fire and forget cache write
        _ = _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
        
        return result;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cachedCustomer = await _cacheService.GetAsync<Customer>(cacheKey);
        if (cachedCustomer != null)
            return cachedCustomer;

        var customer = _dbContext.GetCustomerById(id);
        if (customer == null)
            return null;

        // Fire and forget cache write
        _ = _cacheService.SetAsync(cacheKey, customer, TimeSpan.FromMinutes(CacheDurationMinutes));
        return customer;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        var createdCustomer = _dbContext.AddCustomer(customer);
        
        // Fire and forget cache invalidation
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync(AllCustomersCacheKey);
                await _cacheService.RemoveByPatternAsync($"{PagedCustomersCacheKeyPrefix}*");
            }
            catch { /* Silent fail */ }
        });
        
        return createdCustomer;
    }

    public async Task<bool> UpdateCustomerAsync(int id, Customer customer)
    {
        var result = _dbContext.UpdateCustomer(id, customer);
        if (!result) 
            return false;

        // Fire and forget cache invalidation
        var cacheKey = $"{CacheKeyPrefix}{id}";
        _ = Task.WhenAll(
            _cacheService.RemoveAsync(cacheKey),
            _cacheService.RemoveAsync(AllCustomersCacheKey),
            _cacheService.RemoveByPatternAsync($"{PagedCustomersCacheKeyPrefix}*")
        );
        
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var result = _dbContext.DeleteCustomer(id);
        if (!result) 
            return false;

        // Fire and forget cache invalidation
        var cacheKey = $"{CacheKeyPrefix}{id}";
        _ = Task.WhenAll(
            _cacheService.RemoveAsync(cacheKey),
            _cacheService.RemoveAsync(AllCustomersCacheKey),
            _cacheService.RemoveByPatternAsync($"{PagedCustomersCacheKeyPrefix}*")
        );
        
        return true;
    }

    public async Task<CustomerWithUserDetailsDto?> GetCustomerWithUserDetailsAsync(int customerId)
    {
        var customer = await GetCustomerByIdAsync(customerId);
        if (customer == null)
            return null;

        try
        {
            // Fetch user details from UserService via gRPC
            var response = await _userServiceClient.GetUserDetailsAsync(
                new global::UserService.Protos.GetUserDetailsRequest { UserId = customerId }
            );
            
            return new CustomerWithUserDetailsDto
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Wallet = customer.Wallet,
                CreatedAt = customer.CreatedAt,
                UserRole = response?.Role ?? "User",
                UserStatus = response?.Status ?? "Active"
            };
        }
        catch
        {
            // Fallback to basic details if UserService is unavailable
            return new CustomerWithUserDetailsDto
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Wallet = customer.Wallet,
                CreatedAt = customer.CreatedAt,
                UserRole = "User",
                UserStatus = "Unknown"
            };
        }
    }
}

public class CustomerWithUserDetailsDto
{
    public int CustomerId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Wallet { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserRole { get; set; }
    public string? UserStatus { get; set; }
}
