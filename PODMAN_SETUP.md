# Podman Setup Guide for Snappay

## What is Podman?

Podman (Pod Manager) is a container engine that:
- ✅ **No daemon required** - Runs daemonless (more secure)
- ✅ **Docker compatible** - Uses same images and syntax
- ✅ **Rootless containers** - Better security by default
- ✅ **Works with docker-compose.yml** - Via podman-compose

## Installation

### Windows Installation

#### Option 1: Podman Desktop (Recommended - GUI)
1. Download Podman Desktop: https://podman-desktop.io/
2. Run installer `podman-desktop.exe`
3. Follow installation wizard
4. It includes Podman + WSL 2 integration

#### Option 2: Direct Installation via Winget
```powershell
winget install RedHat.Podman
```

#### Option 3: Chocolatey
```powershell
choco install podman
```

#### Option 4: Direct Download
1. Download from: https://github.com/containers/podman/releases
2. Extract and add to PATH

## 🛡️ No Admin Privileges? (Alternatives to WSL2)

If you cannot install WSL2 due to lack of admin rights, you have two main options:

### 1. Cloud-Based Containers (Recommended)
Use a cloud IDE which provides a pre-configured Linux environment:
- **GitHub Codespaces**: If your project is on GitHub, click "Code" > "Codespaces" > "Create codespace". It supports Docker/Podman out of the box.
- **Gitpod**: Prefix your repository URL with `gitpod.io/#` (e.g., `gitpod.io/#github.com/user/repo`).

### 2. Bare-Metal Execution (No Containers)
Run the services directly on Windows using the .NET CLI. You will only need a Redis instance (you can use a free cloud Redis like **Upstash**).

```powershell
# Run each service in its own terminal
cd UserService && dotnet run
cd CustomerService && dotnet run
cd OrderService && dotnet run
cd InventoryService && dotnet run
```

### Verify Installation

```powershell
podman --version
podman-compose --version
```

Should output:
```
podman version 4.x.x
podman-compose version x.x.x
```

## System Requirements

- **OS**: Windows 10 (Build 19041+) or Windows 11
- **CPU**: 2+ cores
- **RAM**: 4GB minimum (8GB recommended)
- **WSL 2**: Required for Windows (Podman Desktop installs this)

## Configure Podman

### Initialize Podman Machine (Windows)

After installation:
```powershell
# Start Podman machine
podman machine start

# Verify it's running
podman machine list
```

### Check Network Configuration

```powershell
podman network ls
```

You should see `podman` network.

## Running Snappay with Podman

### Using podman-compose (Just like docker-compose)

Navigate to project:
```powershell
cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc
```

Build and start containers:
```powershell
podman-compose up --build
```

Or in background:
```powershell
podman-compose up -d --build
```

### Using Helper Script

```powershell
.\podman-run.ps1 -Action up
```

## Podman vs Docker Commands

| Task | Docker | Podman |
|------|--------|--------|
| Start containers | `docker-compose up` | `podman-compose up` |
| List containers | `docker ps` | `podman ps` |
| View logs | `docker logs` | `podman logs` |
| Stop containers | `docker-compose down` | `podman-compose down` |
| Access shell | `docker exec -it` | `podman exec -it` |
| Build image | `docker build` | `podman build` |

## Podman-Compose Compatibility

Podman includes full compatibility with docker-compose files:
- ✅ Same docker-compose.yml format
- ✅ Works with `podman-compose` command
- ✅ Supports all common features
- ✅ Compatible with volumes, networks, environment variables

## Common Podman Commands

### Container Management

```powershell
# List running containers
podman ps

# List all containers (including stopped)
podman ps -a

# View container logs
podman logs <container-id>
podman logs -f <container-name>  # Follow logs

# Stop containers
podman-compose down

# Stop specific container
podman stop <container-id>

# Remove container
podman rm <container-id>
```

### Image Management

```powershell
# List images
podman images

# Remove image
podman rmi <image-id>

# Prune unused images
podman image prune
```

### Network Management

```powershell
# List networks
podman network ls

# Inspect network
podman network inspect <network-name>
```

### System Information

```powershell
# Check Podman status
podman info

# Verify machine is running
podman machine list

# Check disk usage
podman system df
```

## Podman Advantages over Docker

| Feature | Docker | Podman |
|---------|--------|--------|
| Daemon | Required | Not required |
| Default rootless | No | Yes |
| Security | Daemon runs as root | Runs as regular user |
| Memory usage | Higher (daemon) | Lower |
| Startup time | Slower (start daemon) | Faster |
| Container signals | Daemon forwards | Direct forwarding |
| API compatibility | Yes | Yes |
| Docker Compose | Yes | Yes (via podman-compose) |

## Troubleshooting

### Podman Machine Issues

```powershell
# Check if machine is running
podman machine list

# Start machine
podman machine start

# Stop machine
podman machine stop

# Reset machine (be careful!)
podman machine rm
podman machine init
podman machine start
```

### Connection Refused

```powershell
# Make sure podman machine is running
podman machine start

# Test connection
podman run hello-world
```

### Permission Denied

Podman runs rootless by default, which is more secure:
```powershell
# If you get permission errors, check:
podman info | findstr "rootless"

# Should show: rootless: true
```

### Port Already in Use

```powershell
# Find process using port
netstat -ano | findstr :7000

# Kill process
taskkill /PID <PID> /F
```

### Container Won't Start

```powershell
# Check logs
podman-compose logs

# Check specific service
podman logs customer_service
podman logs customer_dapr
```

## Verify Everything Works

```powershell
# 1. Start containers
podman-compose up -d --build

# 2. Check running containers
podman ps

# 3. Test Redis
podman exec snappay_redis redis-cli ping
# Should return: PONG

# 4. Test microservice
curl http://localhost:7000/health

# 5. Test Dapr sidecar
curl http://localhost:3500/v1.0/healthz

# 6. View logs
podman-compose logs -f
```

## Switching Between Docker and Podman

Both can coexist:
- **docker-compose.yml** - Used by Docker
- **podman-compose** - Reads same docker-compose.yml file

You can use whichever you prefer. Just make sure only one system is running containers to avoid port conflicts.

## Resources

- **Podman Official**: https://podman.io/
- **Podman Desktop**: https://podman-desktop.io/
- **Podman Docs**: https://docs.podman.io/
- **Podman Compose**: https://github.com/containers/podman-compose
- **Podman Architecture**: https://docs.podman.io/en/latest/Introduction.html

## Migration from Docker to Podman

If you have Docker containers running:

```powershell
# Stop Docker containers
docker-compose down

# Start Podman machine
podman machine start

# Start Podman containers (same compose file!)
podman-compose up --build

# Everything works identically
```

The docker-compose.yml file works the same with both Docker and Podman!
