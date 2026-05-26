# Sub-Order & Wallet Blocking Feature Guide

## Overview

This feature implements a wallet-blocking mechanism where customers can add multiple sub-orders before placing an actual order. The blocked amount accumulates and is only deducted from the wallet when an order is confirmed.

## Key Concepts

### 1. Sub-Order
- Partial order amounts added by customers
- Multiple sub-orders can be added for a single customer
- Each sub-order consolidates the `BlockedAmount`
- Sub-orders remain in `Pending` state until the full order is placed

### 2. Blocked Amount
- Total amount accumulated from all sub-orders for a customer
- Stored at customer level (not per order)
- Remains in `Pending` state while accumulating
- Transitions to `Confirmed` when full order is placed
- Triggers wallet deduction through EDA (Event-Driven Architecture)

### 3. Wallet Blocking Condition
```
Condition: Wallet Amount >= Blocked Amount
  ✓ TRUE:  Customer can place actual order → Blocked Amount deducted from wallet
  ✗ FALSE: Customer must continue adding sub-orders until condition is met
```

### 4. Event-Driven Architecture (EDA)
- `OrderStatus` and `BlockedAmount` stored in file: `events/blocked_amounts.txt`
- CustomerService reads file asynchronously (every 5 seconds by default)
- When `OrderStatus = "Confirmed"`, wallet is updated: `Wallet = Wallet - BlockedAmount`
- Events are automatically cleaned up after processing

## File Format

**File:** `events/blocked_amounts.txt` (created automatically in app directory)

**Format:** Pipe-separated values
```
CustomerId|BlockedAmount|OrderStatus|Timestamp
1|250.50|Pending|2025-05-13 10:30:45.123
2|100.00|Confirmed|2025-05-13 10:31:22.456
```

## API Endpoints

### Sub-Orders Service (Order Service)

#### 1. Add Sub-Order
```
POST /api/sub-orders
Content-Type: application/json

{
  "customerId": 1,
  "amount": 50.00
}

Response: 200 OK
{
  "subOrder": {
    "id": 1,
    "customerId": 1,
    "amount": 50.00,
    "createdAt": "2025-05-13T10:30:45.123Z"
  },
  "blockedAmount": 50.00,
  "walletAmount": 500,
  "canPlaceOrder": false,
  "message": "Sub-order added. Blocked amount is still accumulating."
}
```

#### 2. Add Another Sub-Order (Consolidation)
```
POST /api/sub-orders
Content-Type: application/json

{
  "customerId": 1,
  "amount": 200.00
}

Response: 200 OK
{
  "subOrder": {
    "id": 2,
    "customerId": 1,
    "amount": 200.00,
    "createdAt": "2025-05-13T10:31:00.000Z"
  },
  "blockedAmount": 250.00,
  "walletAmount": 500,
  "canPlaceOrder": true,
  "message": "Blocked amount consolidated. You can now place an order."
}
```

#### 3. Get Sub-Orders by Customer
```
GET /api/sub-orders/customer/1

Response: 200 OK
{
  "customerId": 1,
  "subOrders": [
    {
      "id": 1,
      "customerId": 1,
      "amount": 50.00,
      "createdAt": "2025-05-13T10:30:45.123Z"
    },
    {
      "id": 2,
      "customerId": 1,
      "amount": 200.00,
      "createdAt": "2025-05-13T10:31:00.000Z"
    }
  ],
  "blockedAmount": 250.00,
  "walletAmount": 500,
  "canPlaceOrder": true,
  "subOrderCount": 2
}
```

#### 4. Get Blocked Amount Status
```
GET /api/sub-orders/blocked-amount/1

Response: 200 OK
{
  "customerId": 1,
  "blockedAmount": 250.00,
  "walletAmount": 500,
  "canPlaceOrder": true,
  "deficit": 0
}
```

### Place Order (Updated Order Service)

#### Place Order After Consolidation
```
POST /api/orders
Content-Type: application/json

{
  "customerId": 1,
  "totalAmount": 250.00,
  "items": [
    {
      "productName": "Product A",
      "quantity": 2,
      "unitPrice": 100.00
    },
    {
      "productName": "Product B",
      "quantity": 2,
      "unitPrice": 25.00
    }
  ]
}

Response: 201 Created
{
  "id": 1,
  "customerId": 1,
  "totalAmount": 250.00,
  "blockedAmount": 250.00,
  "status": "Confirmed",
  "createdAt": "2025-05-13T10:32:00.000Z",
  "items": [...]
}
```

**What happens internally:**
1. Order received with `totalAmount: 250.00`
2. Blocked amount retrieved: `250.00` (from sub-orders)
3. Wallet checked: `500 >= 250` ✓
4. Order created with `Status: Confirmed`
5. Event written: `CustomerId=1|BlockedAmount=250|Status=Confirmed`
6. Sub-orders cleared from memory
7. CustomerService reads event (async, within 5 seconds)
8. Wallet updated: `500 - 250 = 250`
9. Event deleted from file

## Data Flow Example

### Scenario: Customer with $500 wallet buys $250 worth of products

**Step 1: Customer adds first sub-order ($50)**
```
POST /api/sub-orders
{ "customerId": 1, "amount": 50 }

Result:
- SubOrder created (ID: 1)
- BlockedAmount: 50
- Status: Pending
- Event file: 1|50|Pending|...
- Can Place Order? NO (50 < 500 wallet, but customer decides to consolidate)
```

**Step 2: Customer adds second sub-order ($200)**
```
POST /api/sub-orders
{ "customerId": 1, "amount": 200 }

Result:
- SubOrder created (ID: 2)
- BlockedAmount: 250 (50 + 200)
- Status: Pending
- Event file: 1|250|Pending|...
- Can Place Order? YES (250 <= 500 wallet)
```

**Step 3: Customer places order for $250**
```
POST /api/orders
{
  "customerId": 1,
  "totalAmount": 250,
  "items": [...]
}

Result:
- Order created (ID: 1)
- Order Status: Confirmed
- BlockedAmount: 250 (from consolidated sub-orders)
- Event file: 1|250|Confirmed|...
- Sub-orders cleared
```

**Step 4: CustomerService processes event (async)**
```
Background service (every 5 seconds):
1. Reads event file
2. Finds: 1|250|Confirmed|...
3. Finds Customer 1
4. Updates wallet: 500 - 250 = 250
5. Deletes event from file
```

**Final State:**
```
Customer 1:
- Wallet: 250 (was 500)
- Orders: 1 confirmed order
- Sub-orders: Cleared
```

## Cache Strategy

### Cache Keys
- `blocked_amount:{customerId}` - Individual customer's blocked amount
- TTL: 5 minutes (shorter than order cache)

### Cache Invalidation
- Cache cleared when sub-order is added
- Cache cleared when order is placed
- Cache cleared when blocked amount is cleared

## Error Handling

### Invalid Amount
```
POST /api/sub-orders
{ "customerId": 1, "amount": -50 }

Response: 400 Bad Request
{
  "message": "Amount must be greater than 0"
}
```

### Customer Not Found
```
POST /api/sub-orders
{ "customerId": 999, "amount": 100 }

Response: 400 Bad Request
{
  "message": "Customer 999 not found"
}
```

### Insufficient Wallet
```
POST /api/orders
{
  "customerId": 1,
  "totalAmount": 600,
  "items": [...]
}

Response: 400 Bad Request
{
  "message": "Insufficient funds. Wallet: 500, Order Amount: 600"
}
```

### Order Amount Less Than Blocked Amount
```
Blocked Amount: 250
POST /api/orders
{
  "customerId": 1,
  "totalAmount": 200,
  "items": [...]
}

Response: 400 Bad Request
{
  "message": "Order amount 200 must be >= blocked amount 250"
}
```

## Implementation Details

### File-Based Event Store
- **Location:** `{AppBaseDirectory}/events/blocked_amounts.txt`
- **Thread-Safe:** Uses lock for concurrent read/write
- **Async:** All operations are async-wrapped
- **Auto-Created:** Directory created automatically if missing

### Background Service
- **Service:** `BlockedAmountEventProcessorService`
- **Type:** Hosted service (runs continuously)
- **Poll Interval:** 5 seconds (configurable)
- **Processing:** Only processes "Confirmed" events
- **Cleanup:** Deletes events after successful wallet update

### Database Models
```csharp
public class SubOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerBlockedAmount
{
    public int CustomerId { get; set; }
    public decimal BlockedAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Updated Order model
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BlockedAmount { get; set; }  // NEW
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

## Testing the Feature

### Test Case 1: Basic Sub-Order Flow
```bash
# Add first sub-order
curl -X POST https://localhost:7003/api/sub-orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":1,"amount":50}' \
  --insecure

# Add second sub-order
curl -X POST https://localhost:7003/api/sub-orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":1,"amount":200}' \
  --insecure

# Check blocked amount
curl -X GET https://localhost:7003/api/sub-orders/blocked-amount/1 \
  --insecure

# Place order
curl -X POST https://localhost:7003/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":1,
    "totalAmount":250,
    "items":[{"productName":"Item","quantity":1,"unitPrice":250}]
  }' \
  --insecure

# Wait 5+ seconds, then check customer wallet
curl -X GET https://localhost:7001/api/customers/1 \
  --insecure
```

### Test Case 2: Insufficient Wallet
```bash
# Add sub-orders totaling more than wallet
curl -X POST https://localhost:7003/api/sub-orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":2,"amount":40}' \
  --insecure

# Try to place order (customer 2 has 50 wallet)
curl -X POST https://localhost:7003/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":2,
    "totalAmount":40,
    "items":[{"productName":"Item","quantity":1,"unitPrice":40}]
  }' \
  --insecure
# Should succeed (40 <= 50 wallet)

# But trying to place 60 should fail
curl -X POST https://localhost:7003/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":2,
    "totalAmount":60,
    "items":[{"productName":"Item","quantity":1,"unitPrice":60}]
  }' \
  --insecure
# Should fail with "Insufficient funds"
```

## Configuration

### Event Poll Interval
Edit `BlockedAmountEventProcessorService.cs`:
```csharp
private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);  // Change this value
```

### Cache TTL for Blocked Amount
Edit `SubOrderService.cs`:
```csharp
private const int CacheDurationMinutes = 5;  // Change this value
```

### Event File Location
The event file is created in: `{AppDomain.CurrentDomain.BaseDirectory}/events/blocked_amounts.txt`

For Docker deployment, the file is created in the container's working directory.

## Monitoring

### Check Event File
```powershell
# View events being written
Get-Content "events/blocked_amounts.txt" -Tail 10

# Count events
(Get-Content "events/blocked_amounts.txt" | Measure-Object).Count
```

### Check Logs
Look for messages like:
- "Wrote blocked amount event"
- "Deleted event for customer"
- "Updated customer X wallet from Y to Z"
- "Read N blocked amount events"

### Redis Cache
```bash
redis-cli
KEYS blocked_amount:*
GET blocked_amount:1
```

## Summary

This feature enables:
✅ Multi-part order placement (sub-orders)  
✅ Wallet blocking until order confirmation  
✅ Event-driven wallet updates  
✅ File-based event store (minimal requirement)  
✅ Async processing with background service  
✅ Cache integration for performance  
✅ Full validation and error handling  

The implementation follows EDA patterns with file-based event storage and background processing for real-world scalability considerations.
