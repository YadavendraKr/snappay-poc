#!/usr/bin/env pwsh

# Snappay Microservices Startup Script

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Snappay Microservices Startup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Redis is running
Write-Host "Checking Redis connection..." -ForegroundColor Yellow
try {
    $redisCheck = redis-cli ping 2>&1
    if ($redisCheck -eq "PONG") {
        Write-Host "✓ Redis is running" -ForegroundColor Green
    }
}
catch {
    Write-Host "⚠ Redis is not running" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You can start Redis using:" -ForegroundColor Yellow
    Write-Host "  1. Docker: docker run -d -p 6379:6379 redis:7.2-alpine" -ForegroundColor Gray
    Write-Host "  2. Direct: redis-server (if installed)" -ForegroundColor Gray
    Write-Host "  3. WSL: wsl -d Ubuntu -c `"redis-server`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Continuing anyway - services will fail without Redis..." -ForegroundColor Yellow
    Write-Host ""
}

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build Snappay.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Instructions
Write-Host "To start the services, run in separate terminals:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Terminal 1 - Customer Service:" -ForegroundColor Yellow
Write-Host "  cd CustomerService" -ForegroundColor Gray
Write-Host "  dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "Terminal 2 - Order Service:" -ForegroundColor Yellow
Write-Host "  cd OrderService" -ForegroundColor Gray
Write-Host "  dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "Then access:" -ForegroundColor Cyan
Write-Host "  Customer Service: https://localhost:7001/swagger" -ForegroundColor Gray
Write-Host "  Order Service: https://localhost:7003/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "For testing, see: TESTING.md" -ForegroundColor Gray
Write-Host "For quick start, see: QUICKSTART.md" -ForegroundColor Gray
Write-Host ""
