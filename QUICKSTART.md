# Quick Start Guide

Get the Snappay microservices running in 5 minutes.

## Option 1: Local Development (Fastest)

### Step 1: Start Redis

**Windows (PowerShell):**
```powershell
# Using Docker Desktop
docker run -d -p 6379:6379 redis:7.2-alpine
```

**Alternative - WSL:**
```bash
wsl -d Ubuntu -c "redis-server"
```

### Step 2: Build the Solution

```powershell
cd snappay-poc
dotnet build
```

### Step 3: Start Customer Service (Terminal 1)

```powershell
cd CustomerService
dotnet run
```

Wait for message: `Now listening on: https://localhost:7001`

### Step 4: Start Order Service (Terminal 2)

```powershell
cd OrderService
dotnet run
```

Wait for message: `Now listening on: https://localhost:7003`

### Step 5: Test

Open in browser or use curl:
- Customer Service: https://localhost:7001/swagger
- Order Service: https://localhost:7003/swagger

```bash
# Get customers
curl -X GET "https://localhost:7001/api/customers" --insecure

# Get orders
curl -X GET "https://localhost:7003/api/orders" --insecure
```

✅ **Done!** Both services are running with Redis caching and inter-service communication.

---

## Option 2: Docker Compose (Production-like)

### Step 1: Build and Start

```powershell
cd snappay-poc
docker-compose up --build
```

### Step 2: Wait for Services

```
customer-service | Now listening on: https://0.0.0.0:7001
order-service    | Now listening on: https://0.0.0.0:7003
snappay_redis    | Ready to accept connections
```

### Step 3: Test

```bash
# From your host machine (adjust URLs if needed)
curl -X GET "http://localhost:7001/api/customers" \
  -H "accept: application/json"
```

### Step 4: Stop

```powershell
docker-compose down
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│           Client Application                 │
└─────────────────────────────────────────────┘
           │                    │
           ▼                    ▼
    ┌──────────────┐    ┌──────────────┐
    │ Customer     │    │ Order        │
    │ Service      │    │ Service      │
    │ :7001        │    │ :7003        │
    └──────────────┘    └──────────────┘
           │                    │
           │                    ├──────┐
           │                    │      │
           ▼                    ▼      ▼
    ┌──────────────────────────────────────┐
    │        Redis Cache (6379)             │
    │                                        │
    │ • customer:{id}                        │
    │ • all:customers                        │
    │ • order:{id}                           │
    │ • all:orders                           │
    │ • customer:orders:{id}                 │
    └──────────────────────────────────────┘
```

---

## Key Features At A Glance

✅ **Two Microservices**
- Customer Service: Manages customer data
- Order Service: Manages orders

✅ **Inter-Service Communication**
- Order Service validates customers via HTTP
- Automatic customer validation before order creation
- Error handling for service failures

✅ **Redis Caching**
- Automatic cache on GET operations (15-minute TTL)
- Smart cache invalidation on CREATE/UPDATE/DELETE
- Pattern-based cache clearing

✅ **Transactions**
- Customer validation before order creation
- Consistent data across services
- Cascade cache invalidation

✅ **Production Ready**
- Swagger/OpenAPI documentation
- Structured logging
- Docker support
- Error handling and validation

---

## Common Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run --project CustomerService

# Build Docker image
docker build -f CustomerService/Dockerfile -t customer-service:latest .

# Docker Compose
docker-compose up --build    # Start all services
docker-compose down          # Stop all services
docker-compose logs -f       # View logs

# Redis CLI
redis-cli                    # Connect to Redis
KEYS *                       # List all cache keys
GET customer:1               # Get specific cache entry
MONITOR                      # Watch cache operations
```

---

## API Quick Reference

### Customer Service: https://localhost:7001

```
GET    /api/customers         → Get all customers
GET    /api/customers/1       → Get customer 1
POST   /api/customers         → Create customer
PUT    /api/customers/1       → Update customer 1
DELETE /api/customers/1       → Delete customer 1
```

### Order Service: https://localhost:7003

```
GET    /api/orders                      → Get all orders
GET    /api/orders/1                    → Get order 1
GET    /api/orders/customer/1           → Get customer 1's orders
POST   /api/orders                      → Create order
PUT    /api/orders/1/status             → Update order status
DELETE /api/orders/1                    → Delete order 1
```

---

## Seed Data

### Customers (Pre-loaded)
```json
[
  { "id": 1, "name": "John Doe", "email": "john@example.com", "phone": "123-456-7890" },
  { "id": 2, "name": "Jane Smith", "email": "jane@example.com", "phone": "098-765-4321" }
]
```

### Orders (Pre-loaded)
```json
[
  { "id": 1, "customerId": 1, "totalAmount": 299.99, "status": "Delivered", "items": [...] },
  { "id": 2, "customerId": 2, "totalAmount": 150.00, "status": "Processing", "items": [...] }
]
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Redis connection refused | Ensure Redis is running on port 6379 |
| Port 7001/7003 already in use | Kill process or change port in appsettings |
| Service to service call fails | Check Order Service config points to correct Customer Service URL |
| Cache not working | Restart services, check Redis connection |
| Docker build fails | Ensure Docker is running and working directory is correct |

---

## Next Steps

1. **Test the APIs** - Use Swagger UI or the scripts in [TESTING.md](TESTING.md)
2. **Monitor Caching** - Connect to Redis and watch cache operations
3. **Create Orders** - Validate that Order Service calls Customer Service
4. **Check Logs** - See structured logging in action
5. **Read Full Docs** - See [README.md](README.md) for comprehensive documentation

---

## Need Help?

- **API Documentation:** Open Swagger UI at https://localhost:7001/swagger or https://localhost:7003/swagger
- **Testing Guide:** See [TESTING.md](TESTING.md) for detailed test scenarios
- **Full Documentation:** See [README.md](README.md)
- **Logs:** Check console output from each service for detailed operation logs
