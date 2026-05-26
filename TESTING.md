# Snappay Microservices Testing Script

This script demonstrates the microservices in action with inter-service communication and caching.

## Prerequisites

Ensure both services are running:
1. Customer Service: https://localhost:7001
2. Order Service: https://localhost:7003
3. Redis: localhost:6379

## Test Scenarios

### Scenario 1: Get Existing Customers (with caching)

**First Call** - Fetches from database, stores in cache

```bash
curl -X GET "https://localhost:7001/api/customers" \
  -H "accept: application/json" \
  --insecure
```

Response: Returns 2 pre-loaded customers. Check Customer Service logs for "Customers retrieved from cache" on subsequent calls.

### Scenario 2: Get Specific Customer

```bash
curl -X GET "https://localhost:7001/api/customers/1" \
  -H "accept: application/json" \
  --insecure
```

Response:
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "123-456-7890",
  "createdAt": "2025-05-05T10:00:00Z"
}
```

### Scenario 3: Create New Customer

```bash
curl -X POST "https://localhost:7001/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "phone": "555-123-4567"
  }' \
  --insecure
```

Response: 201 Created with new customer (cache invalidated for all:customers)

### Scenario 4: Get Orders for a Customer (Inter-Service Call)

This demonstrates Order Service calling Customer Service to validate the customer exists.

```bash
curl -X GET "https://localhost:7003/api/orders/customer/1" \
  -H "accept: application/json" \
  --insecure
```

Response:
```json
[
  {
    "id": 1,
    "customerId": 1,
    "totalAmount": 299.99,
    "status": "Delivered",
    "createdAt": "2025-04-30T10:00:00Z",
    "items": [
      {
        "id": 1,
        "productName": "Laptop",
        "quantity": 1,
        "unitPrice": 299.99
      }
    ]
  }
]
```

**Watch Order Service Logs:** You'll see:
- "Validating customer..." 
- "Fetching customer 1 from Customer Service" (first time)
- "Orders for customer 1 retrieved from cache" (subsequent calls)

### Scenario 5: Create Order (Transaction - Service Validation)

This demonstrates a transaction where Order Service validates the customer exists before creating an order.

```bash
curl -X POST "https://localhost:7003/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "totalAmount": 150.00,
    "status": "Pending",
    "items": [
      {
        "productName": "Keyboard",
        "quantity": 1,
        "unitPrice": 100.00
      },
      {
        "productName": "Mouse",
        "quantity": 2,
        "unitPrice": 25.00
      }
    ]
  }' \
  --insecure
```

Response: 201 Created with new order

**Transaction Details:**
1. Order Service validates customer ID 1 exists (calls Customer Service)
2. Creates order with given items
3. Invalidates caches: `all:orders` and `customer:orders:1`

### Scenario 6: Create Order with Invalid Customer (Error Handling)

```bash
curl -X POST "https://localhost:7003/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 999,
    "totalAmount": 50.00,
    "status": "Pending",
    "items": [
      {"productName": "Item", "quantity": 1, "unitPrice": 50.00}
    ]
  }' \
  --insecure
```

Response: 400 Bad Request - "Customer 999 not found"

### Scenario 11: Create Order with Insufficient Funds (Wallet Check)

This demonstrates the wallet validation logic.

```bash
curl -X POST "https://localhost:7003/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 2,
    "totalAmount": 1000.00,
    "status": "Pending",
    "items": [{"productName": "Expensive Item", "quantity": 1, "unitPrice": 1000.00}]
  }' \
  --insecure
```

Response: 400 Bad Request - "Insufficient Fund"

### Scenario 7: Update Order Status

```bash
curl -X PUT "https://localhost:7003/api/orders/1/status" \
  -H "Content-Type: application/json" \
  -d '{"status": "Confirmed"}' \
  --insecure
```

Response: 204 No Content (cache invalidated for order:1 and all:orders)

### Scenario 8: Update Customer (Cache Invalidation)

```bash
curl -X PUT "https://localhost:7001/api/customers/1" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe Updated",
    "email": "john.updated@example.com",
    "phone": "555-987-6543"
  }' \
  --insecure
```

Response: 204 No Content (caches invalidated for customer:1 and all:customers)

### Scenario 9: Redis Cache Verification

Check Redis cache contents:

```bash
# Connect to Redis
redis-cli

# View all keys
KEYS *

# Get specific cache entry
GET customer:1
GET all:customers

# Monitor cache operations
MONITOR
```

### Scenario 10: Delete Resources (Cascade Cache Invalidation)

Delete customer:
```bash
curl -X DELETE "https://localhost:7001/api/customers/1" \
  --insecure
```

Delete order:
```bash
curl -X DELETE "https://localhost:7003/api/orders/1" \
  --insecure
```

Response: 204 No Content (relevant caches invalidated)

## Expected Cache Keys in Redis

After running various scenarios:

```
customer:1
customer:2
customer:3
all:customers
order:1
order:2
order:3
all:orders
customer:orders:1
customer:orders:2
customer:orders:3
```

## Performance Testing

### Test Cache Hit Performance

Run the same GET request multiple times:

```bash
# First call - from database
time curl -X GET "https://localhost:7001/api/customers" --insecure

# Second call - from cache (should be faster)
time curl -X GET "https://localhost:7001/api/customers" --insecure
```

Watch the logs:
- First: "Customers retrieved from cache" = FALSE (database hit)
- Second: "Customers retrieved from cache" = TRUE (cache hit)

### Test Inter-Service Communication

Create multiple orders for the same customer:

```bash
for i in {1..5}; do
  curl -X GET "https://localhost:7003/api/orders/customer/1" \
    -H "accept: application/json" \
    --insecure
done
```

First call: "Fetching customer 1 from Customer Service"
Subsequent calls: Use cached customer data

## Logs to Monitor

### Customer Service

Look for:
- `"Fetching customer with id: {id}"`
- `"Customer retrieved from cache"` or `"Customer {id} not found"`
- `"Creating new customer"` / `"Updating customer {id}"` / `"Deleting customer {id}"`

### Order Service

Look for:
- `"Validating customer..."`
- `"Fetching customer {id} from Customer Service"`
- `"Orders for customer {customerId} retrieved from cache"`
- `"Creating order for customer {customerId}"`

## Troubleshooting

If services don't communicate:
1. Check both services are running
2. Verify Customer Service URL in Order Service config: https://localhost:7001
3. Check CORS policy is configured
4. Review error logs in Order Service

If cache isn't working:
1. Verify Redis is running
2. Check Connection String: localhost:6379
3. Restart services to re-establish Redis connection
