# Microservices Integration Guide

## Complete System Architecture

This document provides an overview of how all microservices interact in the SnapPay POC system.

## Services Overview

### 1. UserService (Port 5300)
**Role**: Central user profile management via gRPC
- Stores user details: name, email, phone, role, status
- Exposes gRPC endpoints for inter-service communication
- Provides REST API for direct access
- Uses in-memory database (seeded with default users)

### 2. CustomerService (Port 5100)
**Role**: Customer account management with wallet
- Manages customer accounts and wallet balances
- Calls UserService via gRPC to fetch user role and status
- Uses Redis for caching customer data
- Middleware automatically updates wallet on API calls
- REST API for customer CRUD operations

### 3. OrderService (Port 5200)
**Role**: Order management with quantity logging
- Manages orders and sub-orders
- Logs product quantity for each order
- Writes events to blocked_amounts.txt
- No polling services (request-time updates only)

### 4. InventoryService (Port 5200)
**Role**: Product inventory management using CQRS
- Tracks product stock levels
- Reduces stock based on blocked_amounts.txt entries
- Uses CQRS pattern for command/query separation
- Middleware triggers inventory updates on every API call
- Binary protocol for minimal latency

## Service Communication Flow

### Scenario 1: Get Customer with User Role

```
Client Request
    ↓
GET /api/customers/{id}/with-user-details
    ↓
CustomersController
    ↓
CustomerService.GetCustomerWithUserDetailsAsync()
    ├─ Fetch customer from DB/Cache
    └─ Call UserServiceGrpcClient.GetUserDetailsAsync()
        ↓
        UserService (gRPC)
        ├─ Query UserDbContext
        └─ Return UserDetailsResponse
    ↓
Response: CustomerWithUserDetailsDto
(includes customer data + user role + user status)
```

### Scenario 2: Create Order and Update Inventory

```
Client Request
    ↓
POST /api/orders
    ↓
OrdersController
    ↓
OrderService.CreateOrderAsync()
    ├─ Create order with product quantity
    ├─ Log quantity in database
    └─ Write event to blocked_amounts.txt
    ↓
Response: Order Created
```

### Scenario 3: Inventory Stock Update (Automatic via Middleware)

```
Any InventoryService API Call
    ↓
InventoryUpdateMiddleware
    ↓
inventoryService.UpdateStockFromBlockedAmountsAsync()
    ├─ Read blocked_amounts.txt
    ├─ Parse quantity for each product
    └─ Reduce stock in memory
    ↓
Execute Request Handler
```

### Scenario 4: Customer Wallet Update (Automatic via Middleware)

```
Any CustomerService API Call
    ↓
WalletUpdateMiddleware
    ↓
customerService.UpdateWalletFromBlockedAmountsAsync()
    ├─ Read blocked_amounts.txt
    ├─ Calculate total deducted amount
    └─ Update customer wallet
    ↓
Execute Request Handler
```

## Data Flow

### blocked_amounts.txt Format
```
EventId,ProductId,Quantity,OrderId,Timestamp
001,PROD001,2,ORD001,2024-01-01T10:00:00
002,PROD002,5,ORD002,2024-01-01T10:05:00
```

### Service Port Configuration
```
UserService:       http://localhost:5300
CustomerService:   http://localhost:5100
OrderService:      http://localhost:5200
InventoryService:  http://localhost:5200 (co-hosted with OrderService or separate)
```

## Latency Optimization

### 1. gRPC Communication (UserService ↔ CustomerService)
- **Binary Protocol**: Smaller payloads than JSON
- **HTTP/2**: Multiplexing multiple requests
- **Connection Reuse**: Channel pooling reduces connection overhead
- **Expected Latency**: 1-5ms for local calls

### 2. Middleware-Based Updates
- **No Polling**: All updates happen on-demand with requests
- **In-Memory Cache**: FileEventStore caches blocked_amounts data
- **Async Operations**: Non-blocking I/O throughout

### 3. Redis Caching
- **Cache Layer**: Customer data cached for 15 minutes
- **Cache Invalidation**: On create/update/delete operations
- **Pattern-Based Cleanup**: Batch removal of related cache keys

## API Endpoints Summary

### UserService
```
REST:
  GET    /api/users/{id}
  GET    /api/users/email/{email}
  POST   /api/users
  PUT    /api/users/{id}
  DELETE /api/users/{id}

gRPC:
  GetUserDetails(int userId)
  GetUserByEmail(string email)
  CreateUser(name, email, phone, role)
```

### CustomerService
```
GET    /api/customers
GET    /api/customers/paged?page=1&pageSize=10
GET    /api/customers/{id}
GET    /api/customers/{id}/with-user-details    ← NEW: Uses gRPC to fetch user role
POST   /api/customers
PUT    /api/customers/{id}
DELETE /api/customers/{id}
```

### OrderService
```
GET    /api/orders
GET    /api/orders/{id}
GET    /api/orders/paged?page=1&pageSize=10
POST   /api/orders
PUT    /api/orders/{id}
DELETE /api/orders/{id}

GET    /api/suborders
GET    /api/suborders/{id}
POST   /api/suborders
```

### InventoryService
```
GET    /api/inventory/product/{productId}
POST   /api/inventory/reduce-stock
```

## Deployment Considerations

### Local Development
1. Start UserService: `cd UserService && dotnet run`
2. Start CustomerService: `cd CustomerService && dotnet run`
3. Start OrderService: `cd OrderService && dotnet run`
4. Start InventoryService: `cd InventoryService && dotnet run`
5. Redis should be running on localhost:6379

### Docker Deployment
```yaml
services:
  user-service:
    build: ./UserService
    ports: ["5300:5300"]
    
  customer-service:
    build: ./CustomerService
    ports: ["5100:5100"]
    depends_on:
      - user-service
      - redis
      
  order-service:
    build: ./OrderService
    ports: ["5200:5200"]
    
  inventory-service:
    build: ./InventoryService
    ports: ["5201:5201"]
    
  redis:
    image: redis:latest
    ports: ["6379:6379"]
```

## Performance Metrics

### Expected Response Times
- Simple CRUD operations: 5-20ms
- Operations with gRPC call: 10-30ms
- Operations with cache hit: 1-5ms
- Operations with cache miss: 20-50ms

### Resource Usage
- Each service: ~100-200MB RAM
- Redis cache: ~50-100MB
- In-memory databases: Minimal (POC scale)

## Error Handling

### Resilience Patterns
1. **gRPC Retry Logic**: Automatic retries for transient failures
2. **Cache Fallback**: Use cached data if service unavailable
3. **Circuit Breaker**: Prevent cascading failures
4. **Timeout Management**: 5-10 second timeouts on service calls

### Monitoring
- **Logging**: Structured logging in all services
- **Health Checks**: Monitor service availability
- **Performance Metrics**: Track API response times

## Future Enhancements

1. **API Gateway**: Centralized entry point for all services
2. **Service Mesh**: Istio/Linkerd for advanced networking
3. **Message Queue**: Event-driven architecture with RabbitMQ/Kafka
4. **Distributed Tracing**: Jaeger/Zipkin for cross-service observability
5. **Authentication**: JWT tokens with centralized auth service
6. **Rate Limiting**: API rate limiting and throttling
7. **Contract Testing**: Pact for gRPC contract validation

## Troubleshooting

### gRPC Connection Issues
```bash
# Check UserService is running
curl http://localhost:5300/

# Use grpcurl to test gRPC endpoint
grpcurl -plaintext localhost:5300 list
```

### Slow Response Times
1. Check if Redis is running
2. Monitor blocked_amounts.txt file size
3. Check gRPC channel reuse in CustomerService
4. Verify network latency between services

### Data Inconsistencies
1. Review middleware execution order
2. Check file event store synchronization
3. Verify in-memory cache invalidation logic

---

**Last Updated**: January 2024
**Version**: 1.0
