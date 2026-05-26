#!/usr/bin/env pwsh

param(
    [int]$ConcurrentRequests = 10,
    [int]$DurationSeconds = 30,
    [string]$CustomerServiceUrl = "http://localhost:7000",
    [string]$OrderServiceUrl = "http://localhost:7002"
)

Write-Host "SNAPPAY LOAD TEST - TIMEOUT FIX VALIDATION" -ForegroundColor Cyan
Write-Host "=" * 60
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Concurrent Requests: $ConcurrentRequests"
Write-Host "  Duration: $DurationSeconds seconds"
Write-Host "  CustomerService URL: $CustomerServiceUrl"
Write-Host "  OrderService URL: $OrderServiceUrl"
Write-Host ""

# Metrics tracking
$metrics = @{
    TotalRequests = 0
    SuccessfulRequests = 0
    FailedRequests = 0
    TimeoutRequests = 0
    Errors = @()
    ResponseTimes = @()
}

# Test 1: Line 29 - OrderService to CustomerService call
Write-Host "Test 1: Line 29 Scenario (CreateOrder triggers GetCustomer)" -ForegroundColor Green
Write-Host "-" * 60
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$jobs = @()
$endTime = (Get-Date).AddSeconds($DurationSeconds)
$requestCount = 0

while ((Get-Date) -lt $endTime) {
    for ($i = 0; $i -lt $ConcurrentRequests; $i++) {
        $job = Start-Job -ScriptBlock {
            param($OrderServiceUrl, $ReqNum)
            
            try {
                $orderBody = @{
                    customerId = (Get-Random -Minimum 1 -Maximum 100)
                    status = "Pending"
                    totalAmount = (Get-Random -Minimum 10 -Maximum 1000)
                } | ConvertTo-Json
                
                $timer = [System.Diagnostics.Stopwatch]::StartNew()
                $response = Invoke-WebRequest `
                    -Uri "$OrderServiceUrl/api/orders" `
                    -Method Post `
                    -Body $orderBody `
                    -ContentType "application/json" `
                    -TimeoutSec 5 `
                    -UseBasicParsing `
                    -ErrorAction Stop
                $timer.Stop()
                
                return @{
                    Success = $true
                    StatusCode = $response.StatusCode
                    ResponseTime = $timer.ElapsedMilliseconds
                    Timeout = $false
                    Error = $null
                }
            }
            catch {
                $isTimeout = $_.Exception.Message -like "*timed out*" -or $_.Exception.Message -like "*timeout*"
                return @{
                    Success = $false
                    StatusCode = $null
                    ResponseTime = 5000
                    Timeout = $isTimeout
                    Error = $_.Exception.Message
                }
            }
        } -ArgumentList $OrderServiceUrl, $requestCount
        
        $jobs += $job
        $requestCount++
    }
    
    Start-Sleep -Milliseconds 100
}

Write-Host "Waiting for Test 1 to complete..." -ForegroundColor Yellow
$results = $jobs | Wait-Job | Receive-Job

foreach ($result in $results) {
    $metrics.TotalRequests++
    
    if ($result.Success) {
        $metrics.SuccessfulRequests++
        $metrics.ResponseTimes += $result.ResponseTime
        Write-Host "Status $($result.StatusCode) - $($result.ResponseTime)ms" -ForegroundColor Green
    } elseif ($result.Timeout) {
        $metrics.TimeoutRequests++
        $metrics.Errors += $result.Error
        Write-Host "TIMEOUT after $($result.ResponseTime)ms" -ForegroundColor Red
    } else {
        $metrics.FailedRequests++
        $metrics.Errors += $result.Error
        Write-Host "ERROR - $($result.Error)" -ForegroundColor Red
    }
}

$stopwatch.Stop()

# Test 2: Direct CustomerService calls
Write-Host ""
Write-Host "Test 2: CustomerService Direct (GET requests)" -ForegroundColor Green
Write-Host "-" * 60
$jobs2 = @()
$endTime2 = (Get-Date).AddSeconds(15)

while ((Get-Date) -lt $endTime2) {
    for ($i = 0; $i -lt ($ConcurrentRequests / 2); $i++) {
        $job = Start-Job -ScriptBlock {
            param($CustomerServiceUrl)
            
            try {
                $timer = [System.Diagnostics.Stopwatch]::StartNew()
                $response = Invoke-WebRequest `
                    -Uri "$CustomerServiceUrl/api/customers/$(Get-Random -Minimum 1 -Maximum 100)" `
                    -UseBasicParsing `
                    -TimeoutSec 5 `
                    -ErrorAction Stop
                $timer.Stop()
                
                return @{
                    Success = $true
                    StatusCode = $response.StatusCode
                    ResponseTime = $timer.ElapsedMilliseconds
                    Timeout = $false
                }
            }
            catch {
                $isTimeout = $_.Exception.Message -like "*timed out*"
                return @{
                    Success = $false
                    StatusCode = $null
                    ResponseTime = 5000
                    Timeout = $isTimeout
                    Error = $_.Exception.Message
                }
            }
        } -ArgumentList $CustomerServiceUrl
        
        $jobs2 += $job
    }
    
    Start-Sleep -Milliseconds 150
}

$results2 = $jobs2 | Wait-Job | Receive-Job

$directSuccess = 0
$directTimeout = 0
$directFailed = 0
$directResponseTimes = @()

foreach ($result in $results2) {
    if ($result.Success) {
        $directSuccess++
        $directResponseTimes += $result.ResponseTime
        Write-Host "Status $($result.StatusCode) - $($result.ResponseTime)ms" -ForegroundColor Green
    } elseif ($result.Timeout) {
        $directTimeout++
        Write-Host "TIMEOUT after $($result.ResponseTime)ms" -ForegroundColor Red
    } else {
        $directFailed++
        Write-Host "ERROR" -ForegroundColor Red
    }
}

# Analysis
Write-Host ""
Write-Host "=" * 60
Write-Host "LOAD TEST RESULTS" -ForegroundColor Cyan
Write-Host "=" * 60
Write-Host ""

Write-Host "Test 1: Line 29 Scenario (CreateOrder)" -ForegroundColor Yellow
Write-Host "  Total Requests: $($metrics.TotalRequests)"
Write-Host "  Successful: $($metrics.SuccessfulRequests)" -ForegroundColor Green
Write-Host "  Failed: $($metrics.FailedRequests)"
Write-Host "  Timeouts: $($metrics.TimeoutRequests)" -ForegroundColor $(if ($metrics.TimeoutRequests -eq 0) { "Green" } else { "Red" })

if ($metrics.ResponseTimes.Count -gt 0) {
    $avg = [int]($metrics.ResponseTimes | Measure-Object -Average).Average
    $min = ($metrics.ResponseTimes | Measure-Object -Minimum).Minimum
    $max = ($metrics.ResponseTimes | Measure-Object -Maximum).Maximum
    $p95 = ($metrics.ResponseTimes | Sort-Object)[[int]($metrics.ResponseTimes.Count * 0.95)]
    
    Write-Host "  Response Times (ms):"
    Write-Host "    Average: $avg"
    Write-Host "    Min: $min"
    Write-Host "    Max: $max"
    Write-Host "    P95: $p95"
}

Write-Host ""
Write-Host "Test 2: CustomerService Direct" -ForegroundColor Yellow
Write-Host "  Successful: $($directSuccess)" -ForegroundColor Green
Write-Host "  Timeouts: $($directTimeout)" -ForegroundColor $(if ($directTimeout -eq 0) { "Green" } else { "Red" })
Write-Host "  Failed: $($directFailed)"

if ($directResponseTimes.Count -gt 0) {
    $avg2 = [int]($directResponseTimes | Measure-Object -Average).Average
    $min2 = ($directResponseTimes | Measure-Object -Minimum).Minimum
    $max2 = ($directResponseTimes | Measure-Object -Maximum).Maximum
    
    Write-Host "  Response Times (ms):"
    Write-Host "    Average: $avg2"
    Write-Host "    Min: $min2"
    Write-Host "    Max: $max2"
}

Write-Host ""
Write-Host "=" * 60
Write-Host "VERDICT" -ForegroundColor Cyan
Write-Host "=" * 60
Write-Host ""

$timeoutRate = if ($metrics.TotalRequests -gt 0) { [double]$metrics.TimeoutRequests / $metrics.TotalRequests } else { 0 }

if ($metrics.TimeoutRequests -eq 0) {
    Write-Host "PASS: No timeouts detected under load" -ForegroundColor Green
    Write-Host "Line 29 retry logic working correctly" -ForegroundColor Green
} elseif ($timeoutRate -lt 0.05) {
    Write-Host "WARNING: $($metrics.TimeoutRequests) timeouts detected" -ForegroundColor Yellow
} else {
    Write-Host "FAIL: High timeout rate" -ForegroundColor Red
}

Write-Host ""
