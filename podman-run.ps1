#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Podman build and run script for Snappay microservices with Dapr
.DESCRIPTION
    This script builds and runs the Snappay Podman containers with Dapr sidecars.
    Podman is a daemonless container engine (more secure than Docker).
.PARAMETER Action
    The action to perform: 'up', 'down', 'logs', 'rebuild', 'ps', 'stop'
.EXAMPLE
    .\podman-run.ps1 -Action up
    .\podman-run.ps1 -Action down
    .\podman-run.ps1 -Action logs
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('up', 'down', 'logs', 'rebuild', 'ps', 'stop')]
    [string]$Action = 'up'
)

function Check-Podman {
    try {
        $podman = podman --version 2>$null
        $compose = podman-compose --version 2>$null
        
        if (-not $podman -or -not $compose) {
            Write-Host "❌ Podman or Podman Compose is not installed or not in PATH" -ForegroundColor Red
            Write-Host "Please follow the installation guide: PODMAN_SETUP.md" -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "✅ Podman: $podman" -ForegroundColor Green
        Write-Host "✅ Podman Compose: $compose" -ForegroundColor Green
        
        # Check if Podman machine is running
        $machineStatus = podman machine list 2>$null
        if ($machineStatus -like "*running*") {
            Write-Host "✅ Podman Machine: Running" -ForegroundColor Green
        }
        else {
            Write-Host "⚠️  Podman Machine may not be running. Starting..." -ForegroundColor Yellow
            podman machine start
            Start-Sleep -Seconds 3
            Write-Host "✅ Podman Machine: Started" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ Error checking Podman installation: $_" -ForegroundColor Red
        exit 1
    }
}

function Start-Containers {
    Write-Host "`n🚀 Building and starting Snappay containers with Podman..." -ForegroundColor Cyan
    Write-Host "This may take several minutes on first run.`n" -ForegroundColor Gray
    
    podman-compose up --build
}

function Start-Containers-Detached {
    Write-Host "`n🚀 Building and starting Snappay containers in background..." -ForegroundColor Cyan
    
    podman-compose up -d --build
    
    Write-Host "`n✅ Containers started in background" -ForegroundColor Green
    Write-Host "View logs with: podman-compose logs -f" -ForegroundColor Gray
}

function Stop-Containers {
    Write-Host "`n🛑 Stopping containers..." -ForegroundColor Yellow
    
    podman-compose down
    
    Write-Host "`n✅ Containers stopped" -ForegroundColor Green
}

function View-Logs {
    Write-Host "`n📋 Showing container logs (press Ctrl+C to exit)..." -ForegroundColor Cyan
    
    podman-compose logs -f
}

function Rebuild-Containers {
    Write-Host "`n🔨 Rebuilding containers without cache..." -ForegroundColor Cyan
    
    podman-compose up --build --no-cache
}

function List-Containers {
    Write-Host "`n📦 Running containers:" -ForegroundColor Cyan
    
    podman ps
}

# Main execution
Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║      Snappay Microservices Podman Management Script           ║
║                 (Daemonless Container Engine)                 ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Check-Podman

switch ($Action) {
    'up' {
        Start-Containers
    }
    'down' {
        Stop-Containers
    }
    'logs' {
        View-Logs
    }
    'rebuild' {
        Rebuild-Containers
    }
    'ps' {
        List-Containers
    }
    'stop' {
        Stop-Containers
    }
}
