#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Docker build and run script for Snappay microservices with Dapr
.DESCRIPTION
    This script builds and runs the Snappay Docker containers with Dapr sidecars.
.PARAMETER Action
    The action to perform: 'up', 'down', 'logs', 'rebuild'
.EXAMPLE
    .\docker-run.ps1 -Action up
    .\docker-run.ps1 -Action down
    .\docker-run.ps1 -Action logs
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('up', 'down', 'logs', 'rebuild', 'ps', 'stop')]
    [string]$Action = 'up'
)

function Check-Docker {
    try {
        $docker = docker --version 2>$null
        $compose = docker-compose --version 2>$null
        
        if (-not $docker -or -not $compose) {
            Write-Host "❌ Docker or Docker Compose is not installed or not in PATH" -ForegroundColor Red
            Write-Host "Please follow the installation guide: DOCKER_SETUP.md" -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "✅ Docker: $docker" -ForegroundColor Green
        Write-Host "✅ Docker Compose: $compose" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error checking Docker installation: $_" -ForegroundColor Red
        exit 1
    }
}

function Start-Containers {
    Write-Host "`n🚀 Building and starting Snappay containers..." -ForegroundColor Cyan
    Write-Host "This may take several minutes on first run.`n" -ForegroundColor Gray
    
    docker-compose up --build
}

function Start-Containers-Detached {
    Write-Host "`n🚀 Building and starting Snappay containers (background mode)..." -ForegroundColor Cyan
    
    docker-compose up -d --build
    
    Write-Host "`n✅ Containers started in background" -ForegroundColor Green
    Write-Host "View logs with: docker-compose logs -f" -ForegroundColor Gray
}

function Stop-Containers {
    Write-Host "`n🛑 Stopping containers..." -ForegroundColor Yellow
    
    docker-compose down
    
    Write-Host "`n✅ Containers stopped" -ForegroundColor Green
}

function View-Logs {
    Write-Host "`n📋 Showing container logs (press Ctrl+C to exit)..." -ForegroundColor Cyan
    
    docker-compose logs -f
}

function Rebuild-Containers {
    Write-Host "`n🔨 Rebuilding containers without cache..." -ForegroundColor Cyan
    
    docker-compose up --build --no-cache
}

function List-Containers {
    Write-Host "`n📦 Running containers:" -ForegroundColor Cyan
    
    docker ps
}

# Main execution
Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║         Snappay Microservices Docker Management Script        ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Check-Docker

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
