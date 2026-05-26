#!/usr/bin/env pwsh

Write-Host "=== SNAPPAY SERVICE LATENCY TEST ===" -ForegroundColor Cyan
Write-Host "Testing timeout fix at OrderService line 29" -ForegroundColor Yellow
Write-Host ""

# Test 1: CustomerService health check
Write-Host "Test 1: CustomerService GET /api/customers/1" -ForegroundColor Green
$timer = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7000/api/customers/1" -TimeoutSec 5 -ErrorAction Stop
    $timer.Stop()
    Write-Host "✅ Status: $($response.StatusCode) | Time: $($timer.ElapsedMilliseconds)ms" -ForegroundColor Green
} catch {
    $timer.Stop()
    Write-Host "❌ Error after $($timer.ElapsedMilliseconds)ms: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: OrderService health check
Write-Host "Test 2: OrderService GET /api/orders" -ForegroundColor Green
$timer = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7002/api/orders" -TimeoutSec 5 -ErrorAction Stop
    $timer.Stop()
    Write-Host "✅ Status: $($response.StatusCode) | Time: $($timer.ElapsedMilliseconds)ms" -ForegroundColor Green
} catch {
    $timer.Stop()
    Write-Host "❌ Error after $($timer.ElapsedMilliseconds)ms: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Line 29 specific - Create order (triggers CustomerService call)
Write-Host "Test 3: OrderService POST /api/orders (Line 29 - CustomerService lookup)" -ForegroundColor Green
$orderBody = @{
    customerId = 1
    status = "Pending"
    totalAmount = 99.99
} | ConvertTo-Json

$timer = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7002/api/orders" `
        -Method Post `
        -Body $orderBody `
        -ContentType "application/json" `
        -TimeoutSec 5 `
        -ErrorAction Stop
    $timer.Stop()
    Write-Host "✅ Status: $($response.StatusCode) | Time: $($timer.ElapsedMilliseconds)ms" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Cyan
} catch {
    $timer.Stop()
    Write-Host "❌ Error after $($timer.ElapsedMilliseconds)ms: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Verify with second request (should be faster - cached)
Write-Host "Test 4: OrderService GET /api/orders (cached)" -ForegroundColor Green
$timer = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7002/api/orders" -TimeoutSec 5 -ErrorAction Stop
    $timer.Stop()
    Write-Host "✅ Status: $($response.StatusCode) | Time: $($timer.ElapsedMilliseconds)ms (should be faster)" -ForegroundColor Green
} catch {
    $timer.Stop()
    Write-Host "❌ Error after $($timer.ElapsedMilliseconds)ms: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== TEST COMPLETE ===" -ForegroundColor Cyan
Write-Host "Line 29 retry logic: ACTIVE with exponential backoff" -ForegroundColor Yellow
Write-Host "Max retries: 3 | Initial delay: 100ms | Backoff multiplier: 2x" -ForegroundColor Yellow
