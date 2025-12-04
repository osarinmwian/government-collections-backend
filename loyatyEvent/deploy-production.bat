@echo off
echo Deploying KeyLoyalty Service to Production...

REM Create directories
mkdir certs 2>nul
mkdir logs 2>nul

REM Build and publish
cd src\KeyLoyalty.API
dotnet publish -c Release -o ..\..\publish --self-contained -r win-x64

REM Copy configuration
copy appsettings.Production.json ..\..\publish\

REM Set environment variables (update these with your values)
setx DB_SERVER "10.40.14.22,1433" /M
setx DB_NAME "OmniChannelDB2" /M
setx DB_USER "DevSol" /M
setx DB_PASSWORD "DevvSol1234" /M
setx ASPNETCORE_ENVIRONMENT "Production" /M
setx ASPNETCORE_URLS "http://+:5000" /M

REM Install as Windows Service
sc create "KeyLoyalty Service" binPath="%CD%\..\..\publish\KeyLoyalty.API.exe" start=auto DisplayName="KeyMobile Loyalty Service"
sc description "KeyLoyalty Service" "Event-driven loyalty service for KeyMobile transactions"

REM Start service
sc start "KeyLoyalty Service"

echo Production deployment completed!
echo Service URL: http://localhost:5000
echo Health Check: http://localhost:5000/health
echo Logs: .\logs\

pause