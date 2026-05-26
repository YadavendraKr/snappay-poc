@echo off
REM Snappay Microservices Startup Script

echo.
echo ========================================
echo Snappay Microservices Startup Script
echo ========================================
echo.
echo Removing contents of event file
type nul > "C:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc\events\blocked_amounts.txt"

REM Check if Redis is running
echo Checking Redis connection...
redis-cli ping >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Redis is not running!
    echo.
    echo You can start Redis using:
    echo   1. Docker: docker run -d -p 6379:6379 redis:7.2-alpine
    echo   2. Direct: redis-server (if installed)
    echo   3. WSL: wsl -d Ubuntu -c "redis-server"
    echo.
    echo Continuing anyway - services will fail without Redis...
    echo.
)

REM Build the solution
echo Building solution...
dotnet build Snappay.sln
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.

REM Instructions
echo To start the services, run in separate terminals:
echo.
echo Terminal 1 - Customer Service:
echo   cd CustomerService
echo   dotnet run
echo.
echo Terminal 2 - Order Service:
echo   cd OrderService
echo   dotnet run
echo.
echo Then access:
echo   Customer Service: https://localhost:7001/swagger
echo   Order Service: https://localhost:7003/swagger
echo.
echo For testing, see TESTING.md
echo For quick start, see QUICKSTART.md
echo.
