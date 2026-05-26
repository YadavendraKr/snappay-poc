# Snappay Microservices Architecture

## System Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                      Client/Browser                                │
│              (REST API Calls via Swagger/cURL)                     │
└─────────────────┬──────────────────────────────────────────────────┘
                  │
        ┌─────────┴──────────┐
        │                    │
        ▼                    ▼
┌───────────────────┐  ┌──────────────────┐
│  CUSTOMER SERVICE │  │  ORDER SERVICE   │
│  (Port 7001)      │  │  (Port 7003)     │
│                   │  │                  │
│ ┌─────────────┐   │  │ ┌──────────────┐ │
│ │ Controllers │   │  │ │ Controllers  │ │
│ └─────────────┘   │  │ └──────────────┘ │
│       │           │  │        │         │
│       ▼           │  │        ▼         │
│ ┌─────────────┐   │  │ ┌──────────────┐ │
│ │ Services    │   │  │ │ Services     │ │
│ │ (Business   │   │  │ │ (Business    │ │
│ │  Logic)     │   │  │ │  Logic)      │ │
│ └─────────────┘   │  │ └──────────────┘ │
│       │           │  │    │        │    │
│       ▼           │  │    │        ▼    │
│ ┌─────────────┐   │  │    │  ┌─────────────────┐
│ │ In-Memory   │   │  │    │  │ Customer Service│
│ │ DB Context  │   │  │    │  │ HTTP Client     │
│ └─────────────┘   │  │    │  └─────────────────┘
└─────────────────┬─┘  └────┼──────────────────────┘
                  │         │
                  │         └──────────────┐
                  │                        │
                  └────────────┬───────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
                ▼                             ▼
         ┌─────────────────┐         ┌──────────────────┐
         │  Redis Cache    │         │  Cache Service   │
         │ (Port 6379)     │         │  (Shared)        │
         │                 │         │                  │
         │ Keys:           │         │ ┌──────────────┐ │
         │ - customer:{id} │─────────┤ │ Get/Set/Del  │ │
         │ - all:customers │         │ │ Operations   │ │
         │ - order:{id}    │         │ └──────────────┘ │
         │ - all:orders    │         └──────────────────┘
         │ - customer:     │
         │   orders:{id}   │
         └─────────────────┘
```

## Service Communication Flow

### 1. Customer Service Standalone Operations

```
Client
  │
  ├─→ GET /api/customers
  │     │
  │     ├─→ Check Cache (all:customers)
  │     │     │
  │     │     ├─→ Cache Hit → Return cached data
  │     │     └─→ Cache Miss → Fetch from DB
  │     │                       │
  │     │                       └─→ Store in Redis
  │     │
  │     └─→ Return response
  │
  ├─→ GET /api/customers/{id}
  │     │
  │     ├─→ Check Cache (customer:{id})
  │     │     │
  │     │     ├─→ Cache Hit → Return
  │     │     └─→ Cache Miss → Fetch from DB → Store in Redis
  │     │
  │     └─→ Return response
  │
  ├─→ POST /api/customers (Create)
  │     │
  │     ├─→ Validate input
  │     ├─→ Save to database
  │     ├─→ Invalidate caches:
  │     │   - all:customers
  │     └─→ Return 201 Created
  │
  ├─→ PUT /api/customers/{id} (Update)
  │     │
  │     ├─→ Validate input
  │     ├─→ Update database
  │     ├─→ Invalidate caches:
  │     │   - customer:{id}
  │     │   - all:customers
  │     └─→ Return 204 No Content
  │
  └─→ DELETE /api/customers/{id}
        │
        ├─→ Remove from database
        ├─→ Invalidate caches:
        │   - customer:{id}
        │   - all:customers
        └─→ Return 204 No Content
```

### 2. Order Service with Inter-Service Communication

```
Client
  │
  ├─→ GET /api/orders/customer/{customerId}
  │     │
  │     ├─→ Validate Customer (calls Customer Service)
  │     │   │
  │     │   ├─→ HTTP GET https://localhost:7001/api/customers/{id}
  │     │   │   │
  │     │   │   └─→ Customer Service response
  │     │   │
  │     │   └─→ Validate exists? → Continue or Return Error
  │     │
  │     ├─→ Check Cache (customer:orders:{id})
  │     │     │
  │     │     ├─→ Cache Hit → Return
  │     │     └─→ Cache Miss → Fetch from DB → Store in Redis
  │     │
  │     └─→ Return orders
  │
  └─→ POST /api/orders (Create)
        │
        ├─→ Validate input
        │
        ├─→ Validate Customer (TRANSACTION)
        │   │
        │   ├─→ HTTP GET https://localhost:7001/api/customers/{customerId}
        │       │
        │       ├─→ If not found → Return 400 Bad Request
        │       └─→ If Wallet < TotalAmount → Return 400 "Insufficient Fund"
        │
        ├─→ Save order to database
        │
        ├─→ Invalidate caches:
        │   - all:orders
        │   - customer:orders:{customerId}
        │
        └─→ Return 201 Created with order
```

## Cache Strategy

### Cache Keys Structure

```
Pattern: namespace:identifier

Examples:
- customer:1           → Individual customer with ID 1
- customer:2           → Individual customer with ID 2
- all:customers        → All customers collection
- order:1              → Individual order with ID 1
- order:2              → Individual order with ID 2
- all:orders           → All orders collection
- customer:orders:1    → All orders for customer ID 1
- customer:orders:2    → All orders for customer ID 2
```

### Cache Invalidation Strategy

```
┌─────────────────────────────────────────────────────────────┐
│                   CACHE INVALIDATION                        │
└─────────────────────────────────────────────────────────────┘

Operation: CREATE Customer
  ├─→ Invalidate: all:customers
  ├─→ TTL: None (immediate invalidation)
  └─→ Reason: New customer should be visible in list

Operation: UPDATE Customer {id}
  ├─→ Invalidate: customer:{id}
  ├─→ Invalidate: all:customers
  └─→ Reason: Updated data must be fresh

Operation: DELETE Customer {id}
  ├─→ Invalidate: customer:{id}
  ├─→ Invalidate: all:customers
  └─→ Reason: Deleted customer removed from all lists

Operation: CREATE Order for Customer {id}
  ├─→ Invalidate: all:orders
  ├─→ Invalidate: customer:orders:{customerId}
  └─→ Reason: New order affects multiple caches

Operation: UPDATE Order Status {id}
  ├─→ Invalidate: order:{id}
  ├─→ Invalidate: all:orders
  └─→ Reason: Status change affects collections

Operation: DELETE Order {id}
  ├─→ Invalidate: order:{id}
  ├─→ Invalidate: all:orders
  ├─→ Invalidate: customer:orders:{customerId}
  └─→ Reason: Affects all relevant collections
```

### Cache Hit vs Miss Flow

```
READ REQUEST
  │
  ├─→ Generate cache key
  │     Example: customer:1
  │
  ├─→ Check Redis
  │   │
  │   ├─→ KEY EXISTS?
  │   │   │
  │   │   ├─ YES (Cache Hit)
  │   │   │   │
  │   │   │   ├─→ Retrieve value from Redis
  │   │   │   ├─→ Deserialize JSON
  │   │   │   ├─→ Log: "Retrieved from cache"
  │   │   │   └─→ Return to client (FAST)
  │   │   │
  │   │   └─ NO (Cache Miss)
  │   │       │
  │   │       ├─→ Query database
  │   │       ├─→ Serialize to JSON
  │   │       ├─→ Store in Redis (TTL: 15 minutes)
  │   │       ├─→ Log: "Retrieved from database"
  │   │       └─→ Return to client (SLOWER)
  │   │
  │   └─→ TTL Expired?
  │       └─→ Treated as Cache Miss on next request
  │
  └─→ Response to Client
```

## Data Flow - Creating an Order (Complete Transaction)

```
Step 1: Client Creates Order
┌─────────────────────────────────────────────────────┐
│ POST /api/orders                                     │
│ {                                                    │
│   "customerId": 1,                                   │
│   "totalAmount": 100.00,                             │
│   "items": [{"productName": "Item", ...}]            │
│ }                                                    │
└─────────────────────────────────────────────────────┘
         │
         ▼
Step 2: Order Service Validates Customer (INTER-SERVICE)
┌─────────────────────────────────────────────────────┐
│ Order Service                                        │
│                                                      │
│ 1. Check if customer exists                         │
│    → HTTP GET to Customer Service                   │
│    → https://localhost:7001/api/customers/1         │
│                                                      │
│ 2. Customer Service returns customer data           │
│    → Cache hit or database query                    │
│                                                      │
│ 3. Validate: Customer exists = TRUE                 │
│    → Proceed to Step 3                              │
│    → If FALSE → Return 400 Bad Request              │
└─────────────────────────────────────────────────────┘
         │
         ▼
Step 3: Create Order in Database
┌─────────────────────────────────────────────────────┐
│ Order Database (In-Memory)                          │
│                                                      │
│ 1. Generate new Order ID                            │
│ 2. Set CreatedAt timestamp                          │
│ 3. Store Order with Items                           │
│ 4. Return created order                             │
└─────────────────────────────────────────────────────┘
         │
         ▼
Step 4: Invalidate Affected Caches
┌─────────────────────────────────────────────────────┐
│ Redis Cache                                          │
│                                                      │
│ Delete keys:                                         │
│ 1. all:orders                                        │
│    → Next GET /api/orders fetches fresh data        │
│                                                      │
│ 2. customer:orders:1                                 │
│    → Next GET /api/orders/customer/1 fetches fresh  │
│                                                      │
│ Note: Doesn't delete individual order:N keys        │
│       → New order not yet cached                    │
└─────────────────────────────────────────────────────┘
         │
         ▼
Step 5: Return Response to Client
┌─────────────────────────────────────────────────────┐
│ HTTP 201 Created                                     │
│ {                                                    │
│   "id": 3,                                           │
│   "customerId": 1,                                   │
│   "totalAmount": 100.00,                             │
│   "status": "Pending",                               │
│   "createdAt": "2025-05-05T12:30:00Z",               │
│   "items": [...]                                     │
│ }                                                    │
└─────────────────────────────────────────────────────┘
```

## Error Handling & Resilience

```
┌──────────────────────────────────────────────────────┐
│           ERROR HANDLING SCENARIOS                   │
└──────────────────────────────────────────────────────┘

Scenario 1: Customer Service Unreachable
  Order Service tries to validate customer
    │
    └─→ HTTP Request fails (network error)
       │
       ├─→ Catch exception
       ├─→ Log error: "Error fetching customer from Customer Service"
       ├─→ Return null
       └─→ Return 400 Bad Request to client

Scenario 2: Invalid Customer ID
  Order Service validates customer
    │
    └─→ Customer Service returns 404
       │
       ├─→ Order Service catches 404
       ├─→ Return false from ValidateCustomerAsync
       └─→ Return 400 Bad Request: "Customer X not found"

Scenario 3: Cache Connection Lost
  Service tries to read/write cache
    │
    └─→ Redis connection fails
       │
       ├─→ Log warning
       ├─→ Fall back to database
       └─→ Operation continues (graceful degradation)

Scenario 4: Invalid Input
  Client sends malformed request
    │
    └─→ Model validation fails
       │
       ├─→ Return 400 Bad Request
       └─→ Include validation error messages
```

## Deployment Architecture (Docker Compose)

```
┌────────────────────────────────────────────────────────┐
│          Docker Host (Snappay Network)                │
│                                                         │
│ ┌────────────────────────────────────────────────────┐ │
│ │ snappay-network (Bridge)                           │ │
│ │                                                     │ │
│ │ ┌─────────────────┐  ┌──────────────────┐          │ │
│ │ │ customer-service│  │  order-service   │          │ │
│ │ │ Container       │  │  Container       │          │ │
│ │ │                 │  │                  │          │ │
│ │ │ Port: 7001 (SSL)│  │ Port: 7003 (SSL) │          │ │
│ │ │ Port: 7000 (HTTP)  │ Port: 7002 (HTTP)│          │ │
│ │ │                 │  │                  │          │ │
│ │ │ Network:        │  │ Network:         │          │ │
│ │ │ redis:6379      │  │ redis:6379       │          │ │
│ │ │                 │  │ customer-service │          │ │
│ │ │                 │  │ :7001            │          │ │
│ │ └─────────────────┘  └──────────────────┘          │ │
│ │         │                    │                      │ │
│ │         └────────┬───────────┘                      │ │
│ │                  │                                  │ │
│ │                  ▼                                  │ │
│ │ ┌──────────────────────────┐                       │ │
│ │ │   Redis Container        │                       │ │
│ │ │   redis:7.2-alpine       │                       │ │
│ │ │                          │                       │ │
│ │ │   Port: 6379             │                       │ │
│ │ │   Volume: redis_data     │                       │ │
│ │ └──────────────────────────┘                       │ │
│ └────────────────────────────────────────────────────┘ │
│         │              │              │                 │
│         ▼              ▼              ▼                 │
│    Port 7001      Port 7003      Port 6379            │
│    (External)     (External)     (Internal)           │
└────────────────────────────────────────────────────────┘
```

## Performance Characteristics

```
┌─────────────────────────────────────────────────────┐
│          CACHE PERFORMANCE IMPACT                   │
└─────────────────────────────────────────────────────┘

Request Type: GET /api/customers

Time Comparison:
┌──────────────────┬─────────────┬──────────────────┐
│ Source           │ First Call  │ Subsequent Calls │
├──────────────────┼─────────────┼──────────────────┤
│ Database (miss)  │ 5-10ms      │ N/A              │
│ Cache (hit)      │ N/A         │ 1-2ms            │
│ Network (inter-  │ 50-100ms    │ 50-100ms         │
│ service)         │             │                  │
└──────────────────┴─────────────┴──────────────────┘

Request Lifecycle:

1st Call (Cache Miss):
  GET /api/customers
    ├─→ Check Redis (miss): 1ms
    ├─→ Query database: 5ms
    ├─→ Store in Redis: 1ms
    └─→ Total: ~7ms

2nd Call (Cache Hit):
  GET /api/customers
    ├─→ Check Redis (hit): 1ms
    └─→ Total: ~1ms

Speed Improvement: 7x faster with caching

Inter-Service Call:
  POST /api/orders (creates order for customer 1)
    ├─→ Validate customer (HTTP): 50-100ms
    ├─→ Create order (database): 5ms
    ├─→ Invalidate caches (Redis): 2ms
    └─→ Total: ~60-110ms
```

## Summary

This microservices architecture demonstrates:

1. **Two Independent Services** - Customer Service and Order Service
2. **Service Communication** - Order Service calls Customer Service for validation
3. **Caching Strategy** - Redis caching with smart invalidation
4. **Transaction Support** - Customer validation before order creation
5. **Scalability** - Services can be scaled independently
6. **Resilience** - Error handling and graceful degradation
7. **Monitoring** - Structured logging for debugging
8. **Containerization** - Docker support for deployment
