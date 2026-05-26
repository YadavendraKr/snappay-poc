using Shared.Models;

namespace OrderService.Models;

public class SubOrderDbContext
{
    private static List<SubOrder> _subOrders = new();
    private static Dictionary<int, decimal> _customerBlockedAmounts = new();
    private static int _nextSubOrderId = 1;

    public SubOrder AddSubOrder(SubOrder subOrder)
    {
        subOrder.Id = _nextSubOrderId++;
        subOrder.CreatedAt = DateTime.UtcNow;
        _subOrders.Add(subOrder);

        // Consolidate blocked amount
        if (!_customerBlockedAmounts.ContainsKey(subOrder.CustomerId))
        {
            _customerBlockedAmounts[subOrder.CustomerId] = 0;
        }
        _customerBlockedAmounts[subOrder.CustomerId] += subOrder.Amount;

        return subOrder;
    }

    public decimal GetBlockedAmount(int customerId)
    {
        return _customerBlockedAmounts.ContainsKey(customerId) 
            ? _customerBlockedAmounts[customerId] 
            : 0;
    }

    public List<SubOrder> GetSubOrdersByCustomerId(int customerId)
    {
        return _subOrders.Where(s => s.CustomerId == customerId).ToList();
    }

    public Dictionary<int, decimal> GetAllBlockedAmounts()
    {
        // Return a copy of all customers with non-zero blocked amounts
        return _customerBlockedAmounts
            .Where(kvp => kvp.Value > 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ClearBlockedAmount(int customerId)
    {
        if (_customerBlockedAmounts.ContainsKey(customerId))
        {
            _customerBlockedAmounts[customerId] = 0;
        }
    }

    public void ResetAllData()
    {
        _subOrders.Clear();
        _customerBlockedAmounts.Clear();
    }
}
