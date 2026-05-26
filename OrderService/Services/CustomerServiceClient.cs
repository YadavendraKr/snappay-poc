using Shared.Models;

namespace OrderService.Services;

public interface ICustomerServiceClient
{
    Task<Customer?> GetCustomerAsync(int customerId);
    Task<bool> ValidateCustomerAsync(int customerId);
}

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;
    private readonly string _customerServiceUrl;
    private readonly int _maxRetries = 3;
    private readonly int _initialDelayMs = 100;

    public CustomerServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _customerServiceUrl = configuration["ServiceUrls:CustomerService"] ?? "http://localhost:7000";
    }

    public async Task<Customer?> GetCustomerAsync(int customerId)
    {
        int delayMs = _initialDelayMs;
        
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_customerServiceUrl}/api/customers/{customerId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    // If it's a 404 or 400, don't retry
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        return null;
                    
                    // For other errors, retry
                    if (attempt < _maxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var customer = System.Text.Json.JsonSerializer.Deserialize<Customer>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return customer;
            }
            catch (TaskCanceledException ex)
            {
                // Timeout - retry with exponential backoff
                _logger.LogWarning($"Timeout calling CustomerService (attempt {attempt + 1}/{_maxRetries}): {ex.Message}");
                if (attempt < _maxRetries - 1)
                {
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                    continue;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error calling CustomerService (attempt {attempt + 1}/{_maxRetries}): {ex.Message}");
                if (attempt < _maxRetries - 1)
                {
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                    continue;
                }
                return null;
            }
        }
        
        return null;
    }

    public async Task<bool> ValidateCustomerAsync(int customerId)
    {
        var customer = await GetCustomerAsync(customerId);
        return customer != null;
    }
}
