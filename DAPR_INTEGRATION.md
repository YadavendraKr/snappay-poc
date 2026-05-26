# Dapr Sidecar Integration Guide

This document explains the Dapr sidecar setup for the Snappay microservices architecture.

## Overview

Dapr (Distributed Application Runtime) has been integrated as a sidecar for each microservice:
- **Customer Service** (Port 3500/3600 for Dapr)
- **Order Service** (Port 3501/3601 for Dapr)
- **Inventory Service** (Port 3502/3602 for Dapr)
- **User Service** (Port 3503/3603 for Dapr)

## Architecture

Each microservice now runs with two containers:
1. **Application Container**: The .NET microservice
2. **Dapr Sidecar Container**: Provides distributed application capabilities

```
┌─────────────────────────────────────────┐
│  Microservice Container                 │
│  (Customer/Order/Inventory/User Service)│
│  Port: 7000/7002/7004/7006              │
│                                         │
│  ↓ HTTP/gRPC                            │
│                                         │
│  Dapr HTTP Sidecar                      │
│  Port: 3500/3501/3502/3503              │
│                                         │
│  ↓ Connects to                          │
│                                         │
│  - State Store (Redis)                  │
│  - Pub/Sub (Redis)                      │
│  - Service-to-Service Invocation        │
└─────────────────────────────────────────┘
```

## Components Configured

### 1. State Store (Redis)
- **Name**: statestore
- **Type**: state.redis
- **Purpose**: Distributed state management across microservices
- **Port**: Redis on 6379

### 2. Pub/Sub (Redis)
- **Name**: pubsub
- **Type**: pubsub.redis
- **Purpose**: Event-driven communication between microservices
- **Port**: Redis on 6379

### 3. Secrets Store
- **Name**: envsecretsstore
- **Type**: secretstores.local.env
- **Purpose**: Secrets management

## Port Mapping

### HTTP Ports (Microservices)
```
Customer Service: 7000
Order Service:    7002
Inventory Service: 7004
User Service:     7006
```

### HTTPS Ports (Microservices)
```
Customer Service: 7001
Order Service:    7003
Inventory Service: 7005
User Service:     7007
```

### Dapr HTTP Ports (Dapr Sidecars)
```
Customer Dapr:   3500
Order Dapr:      3501
Inventory Dapr:  3502
User Dapr:       3503
```

### Dapr gRPC Ports (Dapr Sidecars)
```
Customer Dapr:   3600
Order Dapr:      3601
Inventory Dapr:  3602
User Dapr:       3603
```

## Running the Services

### Docker Compose
```bash
docker-compose up --build
```

This will start:
- Redis server
- All 4 microservices with their Dapr sidecars

### Checking Service Status
```bash
docker ps
```

You should see 4 microservice containers + 4 Dapr sidecar containers running.

## Using Dapr from Your Microservices

### 1. State Management

**C# Example - Save State**:
```csharp
using var client = new System.Net.Http.HttpClient();
var url = "http://localhost:3500/v1.0/state/statestore";
var data = new { key = "user-123", value = new { name = "John" } };
var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
await client.PostAsync(url, content);
```

**C# Example - Get State**:
```csharp
using var client = new System.Net.Http.HttpClient();
var response = await client.GetAsync("http://localhost:3500/v1.0/state/statestore/user-123");
var state = await response.Content.ReadAsStringAsync();
```

### 2. Pub/Sub Messaging

**Publish Event**:
```csharp
using var client = new System.Net.Http.HttpClient();
var url = "http://localhost:3500/v1.0/publish/pubsub/order-events";
var data = new { orderId = "123", status = "created" };
var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
await client.PostAsync(url, content);
```

**Subscribe to Events** (Add to Program.cs):
```csharp
app.MapPost("/dapr/subscribe", async (Dapr.Client.DaprClient daprClient) =>
{
    var subscription = new Dapr.Client.DaprClient.Subscription
    {
        PubsubName = "pubsub",
        Topic = "order-events",
        Route = "/subscribe/order-events"
    };
    // Handle subscription
    return Results.Ok();
});

app.MapPost("/subscribe/order-events", async (string body) =>
{
    // Process event
    return Results.Ok();
});
```

### 3. Service-to-Service Invocation

**Invoke Another Service**:
```csharp
using var client = new System.Net.Http.HttpClient();
var url = "http://localhost:3500/v1.0/invoke/customer-service/method/api/customers/123";
var response = await client.GetAsync(url);
```

## Configuration Files

### dapr/config.yaml
Main Dapr configuration with:
- Logging level
- Tracing (Zipkin integration ready)
- API allowlist
- Access control policies

### dapr/components/
Contains component definitions:
- `state-redis.yaml`: Redis state store
- `pubsub-redis.yaml`: Redis pub/sub
- `secrets.yaml`: Secrets store

## Environment Variables

Each microservice container receives:
```
DAPR_HTTP_PORT=350x (where x is the service number)
DAPR_GRPC_PORT=360x (where x is the service number)
```

## Troubleshooting

### Check Dapr Container Logs
```bash
docker logs customer_dapr
docker logs order_dapr
docker logs inventory_dapr
docker logs user_dapr
```

### Check Application Container Logs
```bash
docker logs customer_service
docker logs order_service
docker logs inventory_service
docker logs user_service
```

### Verify Connectivity
```bash
# From microservice container, test Dapr sidecar
curl http://localhost:3500/v1.0/healthz

# From host machine
curl http://localhost:3500/v1.0/healthz
```

## Next Steps

1. **Install Dapr CLI** (optional, for local development):
   ```bash
   # Windows
   powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"
   ```

2. **Update Microservices**:
   - Install Dapr SDK for .NET: `dotnet add package Dapr.Client`
   - Update controllers to use Dapr client

3. **Configure Additional Components**:
   - Add more state stores (SQL Server, CosmosDB, etc.)
   - Add more pub/sub providers (Kafka, RabbitMQ, etc.)
   - Add bindings for external systems

4. **Enable Observability**:
   - Configure Zipkin for distributed tracing
   - Add Prometheus metrics collection
   - Setup Dapr dashboard for monitoring

## References

- [Dapr Documentation](https://docs.dapr.io)
- [Dapr .NET SDK](https://github.com/dapr/dotnet-sdk)
- [Dapr Components](https://docs.dapr.io/reference/components-reference/)
- [Dapr API Reference](https://docs.dapr.io/reference/api/)
