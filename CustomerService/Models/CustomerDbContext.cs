using Shared.Models;

namespace CustomerService.Models;

public class CustomerDbContext
{
    private static List<Customer> _customers = new()
    {
        new Customer { Id = 1, Name = "John Doe", Email = "john@example.com", Wallet = 500, Phone = "123-456-7890", CreatedAt = DateTime.UtcNow },
        new Customer { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Wallet = 50, Phone = "098-765-4321", CreatedAt = DateTime.UtcNow }
    };

    private static int _nextId = 3;

    // Pagination support
    public (List<Customer> Items, int Total) GetCustomersPaged(int pageNumber = 1, int pageSize = 10)
    {
        var total = _customers.Count;
        var items = _customers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return (items, total);
    }

    public List<Customer> GetAllCustomers() => _customers;

    public Customer? GetCustomerById(int id) => _customers.FirstOrDefault(c => c.Id == id);

    public Customer AddCustomer(Customer customer)
    {
        customer.Id = _nextId++;
        customer.CreatedAt = DateTime.UtcNow;
        _customers.Add(customer);
        return customer;
    }

    public bool UpdateCustomer(int id, Customer updatedCustomer)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return false;

        customer.Name = updatedCustomer.Name;
        customer.Email = updatedCustomer.Email;
        customer.Phone = updatedCustomer.Phone;
        return true;
    }

    public bool DeleteCustomer(int id)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return false;

        _customers.Remove(customer);
        return true;
    }
}
