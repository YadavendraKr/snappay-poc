# Quick Start - Podman Edition

## ⚡ Quick Summary

Your Snappay project is configured to run with **Podman** (a daemonless, more secure container engine) instead of Docker.

## 🎯 Why Podman Instead of Docker?

✅ **No daemon** - More lightweight, better security  
✅ **Rootless** - Containers run as regular user by default  
✅ **Docker compatible** - Uses same images and docker-compose.yml  
✅ **Lower overhead** - Less memory and CPU usage  
✅ **Better integration** - Native WSL 2 support on Windows  

## 📋 Prerequisites Checklist

- [ ] Podman installed
- [ ] Podman Desktop (optional but recommended)
- [ ] WSL 2 enabled (Windows 10/11 only)
- [ ] 4GB+ RAM available
- [ ] Ports 6379, 7000-7007, 3500-3603 available

## 🚀 Step-by-Step Setup

### Step 1: Install Podman

**Option A: Podman Desktop (Recommended - includes GUI)**
1. Download: https://podman-desktop.io/
2. Run installer `podman-desktop.exe`
3. Follow installation wizard
4. Restart computer when prompted

**Option B: Command Line Installation**
```powershell
# Using Winget
winget install RedHat.Podman

# OR using Chocolatey
choco install podman
```

### Step 2: Verify Installation

Open PowerShell and run:
```powershell
podman --version
podman-compose --version
```

Both should show version numbers.

### Step 3: Start Podman Machine

Podman uses a virtual machine (like Docker Desktop):
```powershell
podman machine start
```

Verify it's running:
```powershell
podman machine list
```

### Step 4: Navigate to Project

```powershell
cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc
```

### Step 5: Build and Start Containers

**Option A: Foreground Mode (see output in real-time)**
```powershell
podman-compose up --build
```

**Option B: Background Mode (use separate terminal for logs)**
```powershell
podman-compose up -d --build
podman-compose logs -f
```

**Option C: Using Helper Script**
```powershell
.\podman-run.ps1 -Action up
```

## ✅ Verify Everything is Running

### Check Running Containers
```powershell
podman ps
# or use helper script
.\podman-run.ps1 -Action ps
```

You should see 9 containers:
```
CONTAINER ID   IMAGE                      NAMES
abc123...      daprio/daprd:latest        customer_dapr
def456...      localhost/snappay:latest   customer_service
ghi789...      daprio/daprd:latest        order_dapr
jkl012...      localhost/snappay:latest   order_service
mno345...      daprio/daprd:latest        inventory_dapr
pqr678...      localhost/snappay:latest   inventory_service
stu901...      daprio/daprd:latest        user_dapr
vwx234...      localhost/snappay:latest   user_service
yza567...      redis:7.2-alpine           snappay_redis
```

### Test Services

**Test Redis**:
```powershell
podman exec snappay_redis redis-cli ping
# Should return: PONG
```

**Test Microservice**:
```powershell
curl http://localhost:7000/health
```

**Test Dapr Sidecar**:
```powershell
curl http://localhost:3500/v1.0/healthz
```

## 📊 Container Architecture

```
9 Total Containers:
├── Redis (6379) - State store & Pub/Sub
├── Customer Service (7000) + Dapr Sidecar (3500/3600)
├── Order Service (7002) + Dapr Sidecar (3501/3601)
├── Inventory Service (7004) + Dapr Sidecar (3502/3602)
└── User Service (7006) + Dapr Sidecar (3503/3603)
```

## 🛠️ Common Podman Commands

### Container Management
```powershell
# List running containers
podman ps

# View logs
podman logs -f customer_service

# Stop all containers
podman-compose down

# Stop specific container
podman stop customer_service
```

### Helper Scripts
```powershell
# View logs
.\podman-run.ps1 -Action logs

# Stop containers
.\podman-run.ps1 -Action down

# List containers
.\podman-run.ps1 -Action ps

# Rebuild without cache
.\podman-run.ps1 -Action rebuild
```

## 📊 Port Reference

| Service | HTTP | HTTPS | Dapr HTTP | Dapr gRPC |
|---------|------|-------|-----------|-----------|
| Customer | 7000 | 7001 | 3500 | 3600 |
| Order | 7002 | 7003 | 3501 | 3601 |
| Inventory | 7004 | 7005 | 3502 | 3602 |
| User | 7006 | 7007 | 3503 | 3603 |
| Redis | 6379 | - | - | - |

## 🚨 Troubleshooting

### Podman Machine Not Running
```powershell
podman machine list
podman machine start
```

### "podman: command not found"
- Close and reopen PowerShell
- Verify Podman is in PATH
- Restart computer if needed

### Port Already in Use
```powershell
netstat -ano | findstr :7000
taskkill /PID <PID> /F
```

### Container Won't Start
```powershell
podman-compose logs
podman logs customer_service
```

### Permission Issues
Podman runs rootless by default (more secure):
```powershell
podman info | findstr rootless
# Should show: rootless: true
```

## 🔧 File Structure

```
project/
├── docker-compose.yml          # Works with podman-compose
├── podman-run.ps1              # Helper script (Windows)
├── podman-run.sh               # Helper script (Linux/Mac)
├── PODMAN_SETUP.md             # Detailed Podman guide
├── DAPR_INTEGRATION.md         # Dapr usage guide
├── dapr/
│   ├── config.yaml
│   └── components/
├── CustomerService/Dockerfile
├── OrderService/Dockerfile
├── InventoryService/Dockerfile
└── UserService/Dockerfile
```

## 📚 Additional Resources

- **Podman Setup**: See [PODMAN_SETUP.md](PODMAN_SETUP.md)
- **Dapr Integration**: See [DAPR_INTEGRATION.md](DAPR_INTEGRATION.md)
- **Podman Official**: https://podman.io/
- **Podman Desktop**: https://podman-desktop.io/
- **Podman Docs**: https://docs.podman.io/

## ✨ What You Get

After successful setup:

✅ **Redis** - State store and Pub/Sub backend  
✅ **4 Microservices** - Customer, Order, Inventory, User  
✅ **4 Dapr Sidecars** - For distributed application capabilities  
✅ **Full Networking** - Services can communicate via Dapr  
✅ **Rootless Security** - Containers run as regular user  

## 🎯 Quick Steps Summary

1. Install Podman from https://podman-desktop.io/
2. Run `podman machine start`
3. Run `cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc`
4. Run `podman-compose up --build`
5. Wait 5-10 minutes for first build
6. Verify with `podman ps`
7. Test with `curl http://localhost:7000/health`
8. Check logs with `podman-compose logs -f`

## 💡 Pro Tips

- Podman Desktop provides GUI for managing containers
- Same `docker-compose.yml` works with both Docker and Podman
- `podman-compose` is drop-in replacement for `docker-compose`
- Use `podman-run.ps1` for easy container management
- Rootless mode is more secure than Docker's default

Good luck! 🚀
