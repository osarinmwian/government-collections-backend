@echo off
echo ========================================
echo KeyLoyalty System Startup
echo ========================================

echo Step 1: Setting up database...
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -i setup-database.sql
if %errorlevel% neq 0 (
    echo Database setup failed. Please check connection.
    pause
    exit /b 1
)

echo.
echo Step 2: Building application...
cd src\KeyLoyalty.API
dotnet build --nologo --verbosity minimal
if %errorlevel% neq 0 (
    echo Build failed. Please check for errors.
    pause
    exit /b 1
)

echo.
echo Step 3: Starting API...
echo API will be available at: http://localhost:5000
echo Swagger UI: http://localhost:5000/swagger
echo.
echo Press Ctrl+C to stop the API
dotnet run --no-build