# Docker Setup Guide for Snappay

## Prerequisites

Before building and running containers, you need to install Docker.

## Installation Steps

### 1. Install Docker Desktop for Windows

**Option A: Download from Docker (Recommended)**
1. Download Docker Desktop from: https://www.docker.com/products/docker-desktop
2. Run the installer `DockerDesktop.exe`
3. Follow the installation wizard
4. Restart your computer when prompted
5. Docker will launch automatically after restart

**Option B: Install via Chocolatey (if you have it installed)**
```powershell
choco install docker-desktop
```

**Option C: Install via Winget**
```powershell
winget install Docker.DockerDesktop
```

### 2. Enable WSL 2 (Windows Subsystem for Linux 2)

Docker Desktop for Windows requires WSL 2. Install it if not already present:

```powershell
wsl --install
wsl --update
```

Then restart your computer.

### 3. Configure Docker Desktop

After installation:
1. Open Docker Desktop application
2. Wait for it to fully start (check system tray for Docker icon)
3. Open PowerShell and verify installation:

```powershell
docker --version
docker-compose --version
```

You should see output like:
```
Docker version 26.x.x, build xxxxx
Docker Compose version v2.x.x
```

### 4. Configure WSL Integration (if needed)

If Docker is installed but not working with WSL:
1. Open Docker Desktop settings
2. Go to **Resources > WSL Integration**
3. Enable integration with your WSL distribution
4. Click **Apply & Restart**

## Verify Installation

Run these commands to verify everything is set up correctly:

```powershell
# Check Docker
docker --version

# Check Docker Compose
docker-compose --version

# Test Docker daemon
docker run hello-world
```

## Build and Run Snappay Containers

Once Docker is installed and running:

```powershell
# Navigate to project directory
cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc

# Build and start all containers
docker-compose up --build

# Or run in detached mode (background)
docker-compose up -d --build
```

## Common Docker Commands

### View Running Containers
```powershell
docker ps
```

### View All Containers (including stopped)
```powershell
docker ps -a
```

### View Container Logs
```powershell
# View logs for specific service
docker logs customer_service
docker logs customer_dapr

# Follow logs (live)
docker logs -f customer_service
```

### Stop Containers
```powershell
# Stop all running containers
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Stop specific container
docker stop customer_service
```

### Rebuild Containers
```powershell
# Rebuild without using cache
docker-compose up --build --no-cache

# Rebuild specific service
docker-compose up --build customer-service
```

### Access Container Shell
```powershell
# Access bash/shell in running container
docker exec -it customer_service bash
docker exec -it customer_service powershell
```

## Troubleshooting

### Docker daemon is not running
1. Open Docker Desktop application from Start menu
2. Wait for it to fully initialize
3. Check system tray for Docker icon (whale)

### "docker: command not found"
1. Docker is installed but PowerShell doesn't recognize it
2. Close and reopen PowerShell/Terminal
3. Or restart your computer

### Port already in use
If you get "port X already in use":
```powershell
# Find which process is using the port
netstat -ano | findstr :7000

# Kill process (replace PID with the actual number)
taskkill /PID <PID> /F
```

### WSL 2 not installed
```powershell
# Install WSL 2
wsl --install

# Update WSL
wsl --update

# Restart computer
```

### Container fails to start
1. Check Docker logs: `docker-compose logs`
2. Check individual service logs: `docker logs customer_service`
3. Ensure all Dockerfiles exist in the project
4. Verify docker-compose.yml syntax: `docker-compose config`

## System Requirements

- **OS**: Windows 10 (Build 19041 or higher) or Windows 11
- **CPU**: 2+ cores
- **RAM**: 4GB minimum (8GB recommended)
- **Storage**: 10GB available for Docker images
- **Virtualization**: Must be enabled in BIOS

## Container Architecture

After Docker is running and you start the containers:

```
Redis (6379)
│
├─ Customer Service (7000) + Customer Dapr Sidecar (3500/3600)
├─ Order Service (7002) + Order Dapr Sidecar (3501/3601)
├─ Inventory Service (7004) + Inventory Dapr Sidecar (3502/3602)
└─ User Service (7006) + User Dapr Sidecar (3503/3603)
```

Total: 9 containers (1 Redis + 4 microservices + 4 Dapr sidecars)

## Next Steps

Once Docker is installed and containers are running:

1. **Test connectivity**: `curl http://localhost:7000` (or use Postman)
2. **Check service health**: Visit http://localhost:3500/v1.0/healthz (Dapr health endpoint)
3. **Review logs**: `docker logs customer_service`
4. **Develop and test**: Make code changes and rebuild with `docker-compose up --build`

## Resources

- Docker Documentation: https://docs.docker.com/
- Docker Desktop for Windows: https://docs.docker.com/desktop/install/windows-install/
- WSL 2 Setup: https://docs.microsoft.com/en-us/windows/wsl/install
- Dapr Documentation: https://docs.dapr.io/
