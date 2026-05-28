# Dapr Sidecar Task Assignments

This document defines the specific responsibilities and tasks assigned to each Dapr sidecar service in the Snappay microservices architecture.

---

## Overview

Each microservice has a corresponding Dapr sidecar that provides cross-cutting concerns including:
- **State Management**: Persistent and transient state storage via Redis
- **Pub/Sub Messaging**: Event-driven inter-service communication
- **Service Invocation**: Secure service-to-service communication
- **Distributed Tracing**: Request tracing via Zipkin
- **Configuration**: Centralized configuration management

---

## Service-Specific Dapr Sidecar Tasks

### 1. Customer Service Sidecar (`customer-dapr`)

**App ID**: `customer-service`  
**Port**: `3500` (HTTP), `3600` (gRPC)  
**Primary Service Port**: `7000`

#### Assigned Tasks:
- **State Management**:
  - Store/retrieve customer account information (balance, status, KYC level)
  - Maintain wallet balance snapshots for fast access
  - Cache blocked amount transactions
  - Key prefix: `customer_*`

- **Pub/Sub Event Handling**:
  - **Subscribed Topics**:
    - `order.completed` → Update customer loyalty points
    - `order.failed` → Restore blocked wallet amounts
    - `wallet.deducted` → Log transaction history
    - `inventory.stock-low` → Notify relevant customers
  - **Published Topics**:
    - `wallet.deducted` → Notify on wallet deduction
    - `customer.updated` → Broadcast customer updates
    - `blocked-amount.created` → Notify on transaction blocking

- **Service Invocation**:
  - Call `user-service` for user profile validation
  - Call `order-service` for order history queries

- **Distributed Tracing**:
  - Track all wallet operations
  - Trace blocking/unblocking transactions
  - Monitor gRPC calls to UserService

---

### 2. Order Service Sidecar (`order-dapr`)

**App ID**: `order-service`  
**Port**: `3501` (HTTP), `3601` (gRPC)  
**Primary Service Port**: `7002`

#### Assigned Tasks:
- **State Management**:
  - Store order state (pending, confirmed, shipped, completed)
  - Maintain sub-order relationships
  - Store order metadata (timestamps, customer info)
  - Cache order status for quick lookups
  - Key prefix: `order_*`

- **Pub/Sub Event Handling**:
  - **Subscribed Topics**:
    - `inventory.stock-low` → Delay order processing
    - `inventory.stock-available` → Resume order processing
    - `payment.confirmed` → Proceed with order fulfillment
    - `customer.blocked` → Cancel orders for blocked customers
  - **Published Topics**:
    - `order.created` → Notify system of new orders
    - `order.completed` → Trigger payments and inventory updates
    - `order.failed` → Notify customer and refund
    - `order.status-changed` → Broadcast order status updates

- **Service Invocation**:
  - Call `customer-service` for wallet operations
  - Call `inventory-service` for stock validation
  - Call `user-service` for shipping address validation

- **Distributed Tracing**:
  - Track complete order lifecycle
  - Monitor inter-service calls
  - Trace payment processing

---

### 3. Inventory Service Sidecar (`inventory-dapr`)

**App ID**: `inventory-service`  
**Port**: `3502` (HTTP), `3602` (gRPC)  
**Primary Service Port**: `7004`

#### Assigned Tasks:
- **State Management**:
  - Store product catalog (SKU, name, price)
  - Maintain real-time stock levels
  - Track reserved inventory for pending orders
  - Store inventory transaction logs
  - Key prefix: `inventory_*`

- **Pub/Sub Event Handling**:
  - **Subscribed Topics**:
    - `order.created` → Reserve stock
    - `order.completed` → Finalize stock reduction
    - `order.failed` → Release reserved stock
  - **Published Topics**:
    - `inventory.stock-reserved` → Confirm stock reservation
    - `inventory.stock-reduced` → Confirm stock deduction
    - `inventory.stock-low` → Alert when stock drops below threshold
    - `inventory.stock-available` → Notify when stock is replenished

- **Service Invocation**:
  - Call `order-service` for order status validation
  - Query `customer-service` for promotional pricing

- **Distributed Tracing**:
  - Track all stock movements
  - Monitor inventory transactions
  - Trace reservation/release operations

---

### 4. User Service Sidecar (`user-dapr`)

**App ID**: `user-service`  
**Port**: `3503` (HTTP), `3603` (gRPC)  
**Primary Service Port**: `7006` (HTTP), `5300` (gRPC)

#### Assigned Tasks:
- **State Management**:
  - Store user profiles (name, email, phone)
  - Maintain authentication tokens and sessions
  - Cache user preferences and settings
  - Store KYC verification status
  - Key prefix: `user_*`

- **Pub/Sub Event Handling**:
  - **Subscribed Topics**:
    - `order.completed` → Update user engagement metrics
    - `customer.updated` → Sync customer profile changes
  - **Published Topics**:
    - `user.created` → Notify on new user registration
    - `user.profile-updated` → Broadcast profile changes
    - `user.verified` → Notify on KYC completion

- **Service Invocation**:
  - Serve gRPC requests from `customer-service` for user validation
  - Serve HTTP requests from other services for user data

- **Distributed Tracing**:
  - Track user authentication requests
  - Monitor KYC verification workflows
  - Trace user profile updates

---

## Dapr Components & Configuration

### State Store (Redis)
- **Component**: `statestore`
- **Type**: `state.redis`
- **Address**: `redis:6379`
- **Purpose**: Persistent state management for all services
- **Consistency**: Eventual consistency

### Pub/Sub (Redis)
- **Component**: `pubsub`
- **Type**: `pubsub.redis`
- **Address**: `redis:6379`
- **Consumer ID**: `snappay`
- **Purpose**: Event-driven inter-service communication

### Secrets Store
- **Component**: `envsecretsstore`
- **Type**: `secretstores.local.env`
- **Purpose**: Local environment variable-based secrets

### Distributed Tracing
- **Endpoint**: `http://zipkin:9411/api/v2/spans`
- **Sampling Rate**: 100%
- **Collector**: OpenZipkin
- **Purpose**: Complete request tracing across services

---

## Event Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                          Event Bus (Redis PubSub)                 │
└──────────────────────────────────────────────────────────────────┘
                    ▲         ▲         ▲         ▲
                    │         │         │         │
        ┌───────────┘         │         │         └───────────┐
        │                     │         │                     │
        │   ┌─────────────────┘         └─────────────────┐   │
        │   │                                             │   │
        ▼   ▼                                             ▼   ▼
   ┌─────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐
   │  Customer   │  │    Order     │  │  Inventory   │  │    User     │
   │   Service   │  │   Service    │  │   Service    │  │   Service   │
   └─────────────┘  └──────────────┘  └──────────────┘  └─────────────┘
        │                   │                  │                │
        └─────┬─────────────┼──────────────────┼────────────────┘
              │             │                  │
         State Store    State Store       State Store         State Store
        (Redis KV)     (Redis KV)       (Redis KV)          (Redis KV)
```

---

## Monitoring & Health Checks

Each Dapr sidecar provides health endpoints:
- **Health Check**: `GET http://localhost:{DAPR_HTTP_PORT}/health/outbound`
- **Metadata**: `GET http://localhost:{DAPR_HTTP_PORT}/v1.0/metadata`

### Health Check Script
```bash
# Customer Service
curl http://localhost:3500/health/outbound

# Order Service
curl http://localhost:3501/health/outbound

# Inventory Service
curl http://localhost:3502/health/outbound

# User Service
curl http://localhost:3503/health/outbound
```

---

## Service Port Mapping

| Service | HTTP Port | gRPC Port | Service Port | Sidecar HTTP | Sidecar gRPC |
|---------|-----------|-----------|--------------|--------------|--------------|
| Customer | 7000 | - | 7000 | 3500 | 3600 |
| Order | 7002 | - | 7002 | 3501 | 3601 |
| Inventory | 7004 | - | 7004 | 3502 | 3602 |
| User | 7006 | 5300 | 7006 | 3503 | 3603 |

---

## Starting Services

All services are configured to start via Docker Compose with their sidecars:

```bash
# Start all services with Dapr sidecars
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Clean up volumes
docker-compose down -v
```

---

## Interaction Examples

### Example 1: Order Processing Workflow
1. Client creates order via OrderService (HTTP)
2. OrderService → Dapr invokes CustomerService to check wallet
3. CustomerService → Dapr publishes `order.created` event
4. InventoryService → Dapr subscribes to `order.created`, reserves stock
5. InventoryService → Dapr publishes `inventory.stock-reserved`
6. OrderService → Dapr subscribes to `inventory.stock-reserved`, confirms order
7. OrderService → Dapr publishes `order.completed`
8. CustomerService → Dapr subscribes, updates loyalty points
9. All transactions traced in Zipkin

### Example 2: State Management
1. Service calls Dapr State API: `POST /v1.0/state/statestore`
2. Dapr forwards to Redis with service-specific key prefix
3. State persists across service restarts
4. Service retrieves state: `GET /v1.0/state/statestore/{key}`

---

## Troubleshooting

### Service fails to connect to sidecar
- Check if Dapr sidecar container is running: `docker ps | grep dapr`
- Verify environment variables are set correctly
- Check Docker network connectivity: `docker network inspect snappay-network`

### Pub/Sub events not being received
- Verify Redis is running: `docker exec snappay_redis redis-cli ping`
- Check Dapr logs for subscription errors: `docker logs {service}-dapr`
- Ensure topic name matches exactly in publisher and subscriber

### State not persisting
- Verify Redis persistence: `docker exec snappay_redis redis-cli CONFIG GET appendonly`
- Check Redis data directory permissions
- Monitor Redis memory: `docker exec snappay_redis redis-cli INFO memory`

---

## Next Steps

1. ✅ Deploy services and Dapr sidecars
2. Monitor service health and event flow
3. Implement circuit breaker patterns for service invocation
4. Set up alerts for failed events
5. Configure auto-scaling based on queue depth

