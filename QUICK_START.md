# Quick Start - Build and Run Containers

## ⚡ Quick Summary

Your project is now configured to run with Docker and Dapr sidecars. Follow these steps to get everything running.

## 📋 Prerequisites Checklist

- [ ] Docker Desktop installed
- [ ] WSL 2 enabled (Windows 10/11)
- [ ] Docker daemon running
- [ ] 4GB+ RAM available
- [ ] Ports 6379, 7000-7007, 3500-3603 available

## 🚀 Step-by-Step Setup

### Step 1: Install Docker

1. Download Docker Desktop: https://www.docker.com/products/docker-desktop
2. Run installer and follow instructions
3. Restart computer when prompted

### Step 2: Verify Docker Installation

Open PowerShell and run:
```powershell
docker --version
docker-compose --version
```

Both should show version numbers.

### Step 3: Navigate to Project

```powershell
cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc
```

### Step 4: Build and Start Containers

**Option A: Interactive Mode (recommended for development)**
```powershell
.\docker-run.ps1 -Action up
```

Or directly with Docker Compose:
```powershell
docker-compose up --build
```

**Option B: Background Mode (for production-like setup)**
```powershell
.\docker-run.ps1 -Action up  # Let it start, then Ctrl+C
.\docker-run.ps1 -Action logs  # View logs separately
```

Or with Docker Compose:
```powershell
docker-compose up -d --build
docker-compose logs -f
```

## ✅ Verify Everything is Running

### Check Running Containers
```powershell
.\docker-run.ps1 -Action ps
# or
docker ps
```

You should see 9 containers:
- 1 Redis
- 4 Microservices (Customer, Order, Inventory, User)
- 4 Dapr Sidecars

### Test Services

**Test Redis**:
```powershell
docker exec snappay_redis redis-cli ping
# Should return: PONG
```

**Test Microservice Health**:
```powershell
curl http://localhost:7000/health
```

**Test Dapr Sidecar**:
```powershell
curl http://localhost:3500/v1.0/healthz
```

## 📊 Container Port Reference

| Service | HTTP | HTTPS | Dapr HTTP | Dapr gRPC |
|---------|------|-------|-----------|-----------|
| Customer | 7000 | 7001 | 3500 | 3600 |
| Order | 7002 | 7003 | 3501 | 3601 |
| Inventory | 7004 | 7005 | 3502 | 3602 |
| User | 7006 | 7007 | 3503 | 3603 |
| Redis | 6379 | - | - | - |

## 🛠️ Common Commands

### View Logs
```powershell
# All containers
docker-compose logs -f

# Specific service
docker logs -f customer_service
docker logs -f customer_dapr
```

### Stop Containers
```powershell
.\docker-run.ps1 -Action down
# or
docker-compose down
```

### Rebuild Without Cache
```powershell
.\docker-run.ps1 -Action rebuild
# or
docker-compose up --build --no-cache
```

### Access Container Shell
```powershell
docker exec -it customer_service powershell
docker exec -it customer_service bash
```

## 🔧 File Structure

```
project/
├── docker-compose.yml          # Docker orchestration
├── docker-run.ps1              # PowerShell helper script
├── docker-run.sh               # Bash helper script
├── DOCKER_SETUP.md             # Detailed setup guide
├── DAPR_INTEGRATION.md         # Dapr configuration guide
├── dapr/
│   ├── config.yaml             # Dapr configuration
│   └── components/             # Dapr components
│       ├── state-redis.yaml
│       ├── pubsub-redis.yaml
│       └── secrets.yaml
├── CustomerService/
│   ├── Dockerfile              # Build instructions
│   └── ...
├── OrderService/
│   ├── Dockerfile
│   └── ...
├── InventoryService/
│   ├── Dockerfile
│   └── ...
└── UserService/
    ├── Dockerfile
    └── ...
```

## 🚨 Troubleshooting

### "docker: command not found"
- Restart PowerShell
- Verify Docker Desktop is running
- Check if Docker is in your PATH

### "Port already in use"
```powershell
# Find which process uses the port
netstat -ano | findstr :7000

# Kill the process
taskkill /PID <PID> /F
```

### Container fails to start
```powershell
# Check logs
docker-compose logs

# Check specific service
docker logs customer_service
docker logs customer_dapr

# Verify Dockerfiles exist
dir */Dockerfile
```

### "Cannot connect to Docker daemon"
1. Open Docker Desktop application
2. Wait for it to start
3. Check system tray for Docker icon

### Memory/CPU issues
- Allocate more resources in Docker Desktop settings
- Go to Settings → Resources
- Increase Memory (recommend 4GB+) and CPU cores

## 📚 Additional Resources

- **Docker Setup Guide**: See [DOCKER_SETUP.md](DOCKER_SETUP.md)
- **Dapr Integration**: See [DAPR_INTEGRATION.md](DAPR_INTEGRATION.md)
- **Docker Docs**: https://docs.docker.com/
- **Dapr Docs**: https://docs.dapr.io/

## ✨ What's Included

After successful startup, you have:

✅ **Redis** - State store and Pub/Sub backend  
✅ **Customer Service** - Microservice with Dapr sidecar  
✅ **Order Service** - Microservice with Dapr sidecar  
✅ **Inventory Service** - Microservice with Dapr sidecar  
✅ **User Service** - Microservice with Dapr sidecar  
✅ **Dapr Sidecars** - For distributed application capabilities  
✅ **Networking** - All services can communicate via Dapr

## 🎯 Next Steps

1. ✅ Install Docker
2. ✅ Run `docker-compose up --build`
3. ✅ Verify containers are running
4. ✅ Test service endpoints
5. ✅ Review Dapr integration guide
6. ✅ Start developing!

Good luck! 🚀
