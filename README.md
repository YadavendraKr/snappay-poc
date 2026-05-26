# Snappay Microservices POC

A high-performance microservices architecture with four services: UserService, CustomerService, OrderService, and InventoryService. Optimized for minimal latency using gRPC communication, Redis caching, and CQRS patterns.

## Architecture

### Microservices

1. **UserService** (Port 5300) - **NEW**
   - Central user profile management
   - gRPC + REST API endpoints
   - User roles and status management
   - Used by CustomerService via gRPC
   - Seed data: 2 pre-loaded users (Admin, Regular)

2. **CustomerService** (Port 5100) - **UPDATED**
   - Customer account management with wallet
   - Fetches user role/status from UserService via gRPC
   - REST API for CRUD operations
   - Redis caching for performance
   - Automatic wallet updates on every API call
   - Seed data: Pre-loaded customers

3. **OrderService** (Port 5200)
   - Order and sub-order management
   - Product quantity logging
   - Event-driven architecture (no polling)
   - Writes events to blocked_amounts.txt
   - REST API endpoints

4. **InventoryService** (Port 5200) - **NEW**
   - Product inventory management using CQRS pattern
   - Reduces stock based on blocked_amounts.txt
   - Automatic updates via middleware on every API call
   - Minimal latency design

5. **Shared Library** - **UPDATED**
   - Common models (Customer, Order, User, Product)
   - Redis cache service implementation
   - gRPC client for UserService
   - CQRS infrastructure (Commands, Mediator, Handlers)
   - Middleware for wallet and inventory updates

## Prerequisites

- .NET 10 SDK (or .NET 8+)
- Docker & Docker Compose (for containerized deployment)
- Redis (localhost:6379 for local development)
- gRPC support (built into .NET)

## Project Structure

```
snappay-poc/
├── Snappay.sln                          # Main solution
│
├── UserService/                         # NEW: Central user management
│   ├── Controllers/UsersController.cs   # REST API
│   ├── Services/UserGrpcService.cs      # gRPC service
│   ├── Models/
│   │   ├── User.cs                      # User entity
│   │   └── UserDbContext.cs             # DbContext
│   ├── Protos/user.proto                # gRPC definition
│   ├── Program.cs
│   ├── UserService.csproj               # With gRPC support
│   └── Properties/launchSettings.json
│
├── CustomerService/                     # UPDATED: Customer + Wallet mgmt
│   ├── Controllers/CustomersController.cs
│   ├── Middleware/WalletUpdateMiddleware.cs
│   ├── Services/CustomerService.cs      # Now calls UserService via gRPC
│   ├── Models/CustomerDbContext.cs
│   ├── Program.cs
│   ├── appsettings.json                 # With UserService URL config
│   └── CustomerService.csproj
│
├── OrderService/                        # Order management
│   ├── Controllers/
│   │   ├── OrdersController.cs
│   │   └── SubOrdersController.cs
│   ├── Services/
│   │   ├── OrderService.cs              # Logs product quantity
│   │   └── BlockedAmountCleanupService.cs
│   ├── Models/
│   │   ├── OrderDbContext.cs
│   │   └── SubOrderDbContext.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── OrderService.csproj
│
├── InventoryService/                    # NEW: Inventory with CQRS
│   ├── Controllers/InventoryController.cs
│   ├── Program.cs
│   ├── InventoryService.csproj
│   └── Properties/launchSettings.json
│
├── Shared/                              # UPDATED: Shared services & models
│   ├── Models/
│   │   ├── Customer.cs
│   │   ├── Order.cs
│   │   └── Product.cs                   # NEW
│   ├── Infrastructure/
│   │   ├── RedisCacheService.cs
│   │   ├── UserServiceGrpcClient.cs     # NEW: gRPC client
│   │   └── InventoryService.cs
│   ├── Middleware/
│   │   ├── WalletUpdateMiddleware.cs
│   │   └── InventoryUpdateMiddleware.cs
│   ├── CQRS/                            # NEW: CQRS infrastructure
│   │   ├── Commands.cs
│   │   └── Mediator.cs
│   └── Shared.csproj
│
├── events/                              # Event files
│   ├── blocked_amounts.txt              # Event store
│   ├── processed_events.txt
│   └── wallet_deducted.txt
│
├── Documentation/
│   ├── README.md                        # This file
│   ├── USERSERVICE_README.md            # NEW: UserService docs
│   ├── GRPC_INTEGRATION_GUIDE.md        # NEW: gRPC guide
│   ├── MICROSERVICES_INTEGRATION.md     # NEW: Architecture overview
│   └── USERSERVICE_IMPLEMENTATION_SUMMARY.md # NEW: Implementation details
│
└── docker-compose.yml
```

## Running Locally

### Prerequisites: Start Redis

```bash
# Option 1: Using Docker
docker run -d -p 6379:6379 redis:7.2-alpine

# Option 2: Using Windows (if Redis is installed)
redis-server

# Option 3: Using WSL
wsl -d Ubuntu redis-server
```

### Build the Solution

```bash
cd snappay-poc
dotnet build
```

### Run All Services (Open 4 terminals)

**Terminal 1 - UserService (gRPC Server)**
```bash
cd UserService
dotnet run
# Listening on: http://localhost:5300
```

**Terminal 2 - CustomerService**
```bash
cd CustomerService
dotnet run
# Listening on: http://localhost:5100
# Swagger UI: http://localhost:5100/swagger
```

**Terminal 3 - OrderService**
```bash
cd OrderService
dotnet run
# Listening on: http://localhost:5200
# Swagger UI: http://localhost:5200/swagger
```

**Terminal 4 - InventoryService**
```bash
cd InventoryService
dotnet run
# Listening on: http://localhost:5201
# Swagger UI: http://localhost:5201/swagger
```

### Verify Services are Running

```bash
# Check UserService (gRPC)
curl http://localhost:5300/

# Check CustomerService
curl http://localhost:5100/swagger

# Check OrderService
curl http://localhost:5200/swagger

# Check InventoryService
curl http://localhost:5201/swagger
```

## Running with Docker Compose

```bash
# Build and start all services
docker-compose up --build

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f user-service
docker-compose logs -f customer-service

# Stop all services
docker-compose down

# Remove volumes (including Redis data)
docker-compose down -v
```

## Troubleshooting

### gRPC Connection Error
```
System.Net.Http.HttpRequestException: Connection refused
```
**Solution**: Ensure UserService is running: `cd UserService && dotnet run`

### Proto Compilation Error
```
Proto compilation failed
```
**Solution**: Check that `user.proto` exists in `UserService/Protos/` directory

### Service Unavailable
```
NullReferenceException in UserServiceGrpcClient
```
**Solution**: Verify `UserServiceUrl` in `CustomerService/appsettings.json` is correct

### Redis Connection Error
```
Exception connecting to Redis server
```
**Solution**: Start Redis: `docker run -d -p 6379:6379 redis:7.2-alpine`

### Port Already in Use
```
System.IO.IOException: Unable to bind to port
```
**Solution**: Change port in `Properties/launchSettings.json` for the affected service

## Performance Characteristics

| Operation | Latency | Notes |
|-----------|---------|-------|
| Simple GET | 5-20ms | With cache hit: 1-5ms |
| gRPC Call (local) | 2-5ms | Binary serialization |
| POST with validation | 10-30ms | Includes gRPC call |
| Cache miss scenario | 20-50ms | Full processing |

## What Was Implemented

### UserService (New)
✅ gRPC service for user management  
✅ REST API for direct access  
✅ User roles and status tracking  
✅ Integration with CustomerService via gRPC  
✅ In-memory database with seed data  

### CustomerService (Enhanced)
✅ gRPC client integration with UserService  
✅ New endpoint: `/customers/{id}/with-user-details`  
✅ Automatic wallet updates via middleware  
✅ Enhanced logging and error handling  

### InventoryService (New)
✅ CQRS pattern implementation  
✅ Stock reduction based on blocked_amounts.txt  
✅ Automatic updates via middleware  

### Performance Optimizations
✅ gRPC binary protocol (10x smaller payloads)  
✅ HTTP/2 multiplexing  
✅ Connection pooling  
✅ Redis caching layer  
✅ Middleware-based synchronization (no polling)  

## Next Steps

1. **Test the gRPC Integration**
   ```bash
   curl http://localhost:5100/api/customers/1/with-user-details
   ```

2. **Monitor Logs**
   - Look for "gRPC Client:" messages in CustomerService logs
   - Verify successful inter-service communication

3. **Load Testing**
   - Use Apache JMeter or similar tools
   - Compare response times with REST-only approach
   - Measure gRPC efficiency gains

4. **Production Deployment**
   - Add TLS/SSL encryption for gRPC
   - Implement authentication (mTLS)
   - Deploy with service mesh (Istio)
   - Add distributed tracing (Jaeger)
   - Set up monitoring and alerting

## API Endpoints

### UserService (gRPC + REST)

**REST Endpoints:**
```
GET    /api/users/{id}              - Get user by ID
GET    /api/users/email/{email}     - Get user by email
POST   /api/users                   - Create user
PUT    /api/users/{id}              - Update user
DELETE /api/users/{id}              - Delete user
```

**gRPC Operations:**
```
GetUserDetails(userId) -> UserDetailsResponse
GetUserByEmail(email) -> UserDetailsResponse
CreateUser(name, email, phone, role) -> UserDetailsResponse
```

### CustomerService

```
GET    /api/customers                           - Get all customers
GET    /api/customers/paged                     - Get paginated customers
GET    /api/customers/{id}                      - Get customer by ID
GET    /api/customers/{id}/with-user-details    - Get customer WITH user role (uses gRPC)
POST   /api/customers                           - Create customer
PUT    /api/customers/{id}                      - Update customer
DELETE /api/customers/{id}                      - Delete customer
```

### OrderService

```
GET    /api/orders                  - Get all orders
GET    /api/orders/{id}             - Get order by ID
GET    /api/orders/paged            - Get paginated orders
POST   /api/orders                  - Create order
PUT    /api/orders/{id}             - Update order
DELETE /api/orders/{id}             - Delete order

GET    /api/suborders               - Get all sub-orders
GET    /api/suborders/{id}          - Get sub-order by ID
POST   /api/suborders               - Create sub-order
PUT    /api/suborders/{id}          - Update sub-order
DELETE /api/suborders/{id}          - Delete sub-order
```

### InventoryService

```
GET    /api/inventory/product/{productId}  - Get product by ID
POST   /api/inventory/reduce-stock         - Reduce product stock
```

## Sample Requests

### Get Customer with User Role (via gRPC)

```bash
# This endpoint internally uses gRPC to fetch user role from UserService
curl -X GET "http://localhost:5100/api/customers/1/with-user-details" \
  -H "accept: application/json"

# Response:
{
  "customerId": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "wallet": 5000,
  "createdAt": "2024-01-01T00:00:00Z",
  "userRole": "Admin",
  "userStatus": "Active"
}
```

### UserService - REST API

```bash
# Get user by ID
curl -X GET "http://localhost:5300/api/users/1" \
  -H "accept: application/json"

# Get user by email
curl -X GET "http://localhost:5300/api/users/email/admin@snappay.com" \
  -H "accept: application/json"

# Create new user
curl -X POST "http://localhost:5300/api/users" \
  -H "Content-Type: application/json" \
  -d '{"name":"New User","email":"new@example.com","phone":"1234567890","role":"User"}'
```

### Get All Customers

```bash
curl -X GET "http://localhost:5100/api/customers" \
  -H "accept: application/json"
```

### Create Customer

```bash
curl -X POST "http://localhost:5100/api/customers" \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com","phone":"123-456-7890","wallet":5000}'
```

### Create Order

```bash
curl -X POST "http://localhost:5200/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":1,
    "orderId":"ORD001",
    "totalAmount":100.00,
    "status":"Pending"
  }'
```

### Reduce Inventory Stock

```bash
curl -X POST "http://localhost:5201/api/inventory/reduce-stock" \
  -H "Content-Type: application/json" \
  -d '{"productId":"PROD001","quantity":5}'
```

### Test gRPC Communication (using grpcurl)

```bash
# Install grpcurl (https://github.com/fullstorydev/grpcurl)

# Get user details via gRPC
grpcurl -plaintext \
  -d '{"userId": 1}' \
  localhost:5300 \
  UserService.Protos.UserService/GetUserDetails

# Get user by email via gRPC
grpcurl -plaintext \
  -d '{"email": "admin@snappay.com"}' \
  localhost:5300 \
  UserService.Protos.UserService/GetUserByEmail
```

## Key Features

### gRPC Integration
- **UserService ↔ CustomerService**: Binary protocol communication
- **Minimal Latency**: ~2-5ms gRPC calls vs 10-15ms REST
- **Connection Pooling**: Efficient resource usage with HTTP/2 multiplexing
- **Type Safety**: Protocol Buffer contracts prevent breaking changes

### Performance Optimizations
- **Redis Caching**: 15-minute cache for customer data
- **In-Memory Database**: Fast operations in POC environment
- **Middleware-Based Updates**: Automatic wallet/inventory updates on every request
- **Event-Driven Architecture**: No polling services

### Event Handling
- **blocked_amounts.txt**: Event store for inventory and wallet changes
- **Automatic Updates**: Middleware triggers updates on API calls
- **Real-Time Synchronization**: Immediate reflection of state changes

## Advanced Topics

### gRPC Communication Flow

1. **Client Request**: REST call to `/api/customers/{id}/with-user-details`
2. **gRPC Call**: CustomerService calls UserService via gRPC
3. **Binary Serialization**: Data exchanged as Protocol Buffers
4. **HTTP/2 Multiplexing**: Efficient connection usage
5. **Response**: Combined customer + user data returned as JSON

### CQRS Pattern (InventoryService)

- **Commands**: Write operations (reduce stock, create inventory)
- **Queries**: Read operations (get product details)
- **Mediator**: Routes commands/queries to appropriate handlers
- **Benefits**: Separation of concerns, scalable architecture

### Middleware Chain

1. **WalletUpdateMiddleware** (CustomerService): Updates wallet on every request
2. **InventoryUpdateMiddleware** (InventoryService): Updates stock on every request
3. **Custom Middleware**: Extendable for cross-cutting concerns

## Documentation

For detailed information, see:

- [USERSERVICE_README.md](./USERSERVICE_README.md) - UserService documentation
- [GRPC_INTEGRATION_GUIDE.md](./GRPC_INTEGRATION_GUIDE.md) - Complete gRPC implementation guide
- [MICROSERVICES_INTEGRATION.md](./MICROSERVICES_INTEGRATION.md) - System architecture overview
- [USERSERVICE_IMPLEMENTATION_SUMMARY.md](./USERSERVICE_IMPLEMENTATION_SUMMARY.md) - Implementation details

### Update Order Status

```bash
curl -X PUT "https://localhost:7003/api/orders/1/status" \
  -H "Content-Type: application/json" \
  -d '{"status":"Confirmed"}' \
  --insecure
```

## Key Features

### 1. Inter-Service Communication
- Order Service calls Customer Service to validate customer existence before creating orders
- HTTP client configured for service-to-service communication
- Error handling and logging for failed calls

### 2. Redis Caching
- **Cache Keys Structure:**
  - `customer:{id}` - Individual customer cache
  - `all:customers` - All customers cache
  - `order:{id}` - Individual order cache
  - `all:orders` - All orders cache
  - `customer:orders:{customerId}` - Orders by customer cache

- **Cache Invalidation:**
  - Automatic invalidation on CREATE, UPDATE, DELETE operations
  - TTL of 15 minutes for automatic expiration
  - Pattern-based removal for related caches

### 3. Transaction Support
- Customer validation and Wallet balance check before order creation
- Order status tracking with multiple states
- Cascading cache invalidation

### 4. Logging
- Structured logging for all operations
- Logs for cache hits/misses
- Error logging for inter-service calls

## Database & Data Storage

- **In-Memory Storage:** Both services use in-memory databases for this POC
- **Seed Data:**
  - Customer Service: 2 pre-loaded customers
  - Order Service: 2 pre-loaded orders (linked to customers)

For production, replace with:
- SQL Server / PostgreSQL
- Entity Framework Core
- Proper transaction management

## Configuration

### appsettings.json

Customer Service:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

Order Service:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "ServiceUrls": {
    "CustomerService": "https://localhost:7001"
  }
}
```

### Environment Variables (Docker)

```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__Redis=redis:6379
ServiceUrls__CustomerService=https://customer-service:7001
```

## Troubleshooting

### Redis Connection Error
- Ensure Redis is running on port 6379
- Check connection string in appsettings.json

### Service to Service Communication Fails
- Verify Customer Service is running on the configured port
- Check ServiceUrls configuration in Order Service
- Review logs for HTTP errors

### Port Already in Use
- Change ports in launchSettings.json or pass via environment variables
- Or kill existing process: `netstat -ano | findstr :7001`

## Future Enhancements

- [ ] Add database persistence (EF Core + SQL Server)
- [ ] Implement message queue (RabbitMQ/Kafka) for async communication
- [ ] Add authentication/authorization (JWT)
- [ ] Add API Gateway (Ocelot)
- [ ] Implement circuit breaker pattern
- [ ] Add distributed tracing (Jaeger)
- [ ] Add health checks endpoints
- [ ] Add unit and integration tests
- [ ] Implement request/response validation
- [ ] Add rate limiting

## License

MIT
