namespace Shared.Models;

public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Wallet { get; set; } // Add this property
    public DateTime CreatedAt { get; set; }
}
