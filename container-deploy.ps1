#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Snappay Container Deployment Script
    
.DESCRIPTION
    Deploys Snappay microservices to Podman containers
    Requires admin privileges to set up Podman machine
    
.PARAMETER Action
    Action to perform: 'up', 'down', 'restart', 'logs', 'clean'
    
.PARAMETER Profile
    Environment profile: 'dev', 'staging', 'prod'
    
.EXAMPLE
    .\container-deploy.ps1 -Action up -Profile dev
#>

param(
    [ValidateSet('up', 'down', 'restart', 'logs', 'clean')]
    [string]$Action = 'up',
    
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Profile = 'dev'
)

# Configuration
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$composeFile = Join-Path $projectRoot "docker-compose.yml"
$daprConfigDir = Join-Path $projectRoot "dapr"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       SNAPPAY CONTAINER DEPLOYMENT SCRIPT                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project Root: $projectRoot"
Write-Host "Docker Compose File: $composeFile"
Write-Host "Profile: $Profile"
Write-Host "Action: $Action"
Write-Host ""

# Function to check prerequisites
function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    # Check Podman
    if (!(Get-Command podman -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: Podman not found. Please install Podman." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Podman: OK" -ForegroundColor Green
    
    # Check podman-compose
    if (!(Get-Command podman-compose -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: podman-compose not found. Install with: pip install podman-compose" -ForegroundColor Red
        exit 1
    }
    Write-Host "  podman-compose: OK" -ForegroundColor Green
    
    # Check docker-compose.yml
    if (!(Test-Path $composeFile)) {
        Write-Host "ERROR: docker-compose.yml not found at $composeFile" -ForegroundColor Red
        exit 1
    }
    Write-Host "  docker-compose.yml: OK" -ForegroundColor Green
    
    Write-Host ""
}

# Function to initialize Podman machine (requires admin)
function Initialize-PodmanMachine {
    Write-Host "Initializing Podman machine..." -ForegroundColor Yellow
    
    $machines = & podman machine list --quiet 2>&1
    
    if ($machines -contains "podman-machine-default" -or $machines -like "*default*") {
        Write-Host "Podman machine already exists" -ForegroundColor Green
    } else {
        Write-Host "Creating new Podman machine..." -ForegroundColor Cyan
        & podman machine init
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to initialize Podman machine" -ForegroundColor Red
            Write-Host "NOTE: This requires admin privileges" -ForegroundColor Yellow
            exit 1
        }
    }
    
    # Start machine
    Write-Host "Starting Podman machine..." -ForegroundColor Cyan
    $machineStatus = & podman machine list --format "{{.Running}}" 2>&1
    
    if ($machineStatus -notcontains "true") {
        & podman machine start
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to start Podman machine" -ForegroundColor Red
            exit 1
        }
        Start-Sleep -Seconds 5
    }
    
    Write-Host "Podman machine ready" -ForegroundColor Green
    Write-Host ""
}

# Function to build and start containers
function Start-Containers {
    Write-Host "Building and starting containers..." -ForegroundColor Yellow
    
    $env:PROFILE = $Profile
    
    Write-Host "Running: podman-compose -f $composeFile up --build -d" -ForegroundColor Cyan
    & podman-compose -f $composeFile up --build -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Containers started successfully" -ForegroundColor Green
        Write-Host ""
        Write-Host "Waiting for services to initialize..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        Show-ServiceStatus
    } else {
        Write-Host "ERROR: Failed to start containers" -ForegroundColor Red
        exit 1
    }
}

# Function to stop containers
function Stop-Containers {
    Write-Host "Stopping containers..." -ForegroundColor Yellow
    
    & podman-compose -f $composeFile down
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Containers stopped successfully" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Failed to stop containers" -ForegroundColor Red
        exit 1
    }
}

# Function to restart containers
function Restart-Containers {
    Write-Host "Restarting containers..." -ForegroundColor Yellow
    
    & podman-compose -f $composeFile restart
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Containers restarted successfully" -ForegroundColor Green
        Write-Host ""
        Show-ServiceStatus
    } else {
        Write-Host "ERROR: Failed to restart containers" -ForegroundColor Red
        exit 1
    }
}

# Function to show logs
function Show-Logs {
    Write-Host "Fetching container logs..." -ForegroundColor Yellow
    
    & podman-compose -f $composeFile logs -f
}

# Function to show service status
function Show-ServiceStatus {
    Write-Host "Service Status:" -ForegroundColor Cyan
    Write-Host "─" * 60
    
    Write-Host ""
    Write-Host "API Endpoints:" -ForegroundColor Green
    Write-Host "  CustomerService:  http://localhost:7000"
    Write-Host "  CustomerService:  https://localhost:7001 (gRPC)"
    Write-Host "  OrderService:     http://localhost:7002"
    Write-Host "  OrderService:     https://localhost:7003 (gRPC)"
    Write-Host ""
    Write-Host "Infrastructure:" -ForegroundColor Green
    Write-Host "  Redis:            localhost:6379"
    Write-Host "  Dapr (Customer):  localhost:3500 (HTTP)"
    Write-Host "  Dapr (Customer):  localhost:3600 (gRPC)"
    Write-Host "  Dapr (Order):     localhost:3501 (HTTP)"
    Write-Host "  Dapr (Order):     localhost:3601 (gRPC)"
    Write-Host ""
    
    Write-Host "Container Status:" -ForegroundColor Cyan
    & podman-compose -f $composeFile ps
    
    Write-Host ""
    Write-Host "Health Checks:" -ForegroundColor Green
    
    try {
        $cs = Invoke-WebRequest -Uri "http://localhost:7000/api/customers/1" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        Write-Host "  CustomerService:  " -NoNewline
        Write-Host "OK (HTTP $($cs.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "  CustomerService:  " -NoNewline
        Write-Host "UNAVAILABLE" -ForegroundColor Red
    }
    
    try {
        $os = Invoke-WebRequest -Uri "http://localhost:7002/api/orders" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        Write-Host "  OrderService:     " -NoNewline
        Write-Host "OK (HTTP $($os.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "  OrderService:     " -NoNewline
        Write-Host "UNAVAILABLE" -ForegroundColor Red
    }
}

# Function to clean up
function Clean-Containers {
    Write-Host "Cleaning up containers and volumes..." -ForegroundColor Yellow
    
    & podman-compose -f $composeFile down -v
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Containers and volumes cleaned successfully" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Failed to clean containers" -ForegroundColor Red
        exit 1
    }
}

# Main execution
try {
    Test-Prerequisites
    
    switch ($Action) {
        'up' {
            Initialize-PodmanMachine
            Start-Containers
        }
        'down' {
            Stop-Containers
        }
        'restart' {
            Restart-Containers
        }
        'logs' {
            Show-Logs
        }
        'clean' {
            Clean-Containers
        }
    }
    
    Write-Host ""
    Write-Host "✅ Operation completed successfully" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
