# KeyLoyalty Service - Deployment Guide

## Overview
Event-driven loyalty service for KeyMobile app that processes transactions from existing databases and awards loyalty points.

## Architecture
- **Domain Layer**: Business events and entities
- **Infrastructure Layer**: Database services and event handling
- **API Layer**: REST endpoints and dependency injection
- **Polling Service**: Monitors transaction databases every 5 minutes

## Prerequisites
- .NET 9.0 SDK
- SQL Server access to OmniChannelDB2
- Windows Server (for Windows Service deployment)
- PowerShell (for certificate generation)

## Quick Start

### Development
```bash
cd src/KeyLoyalty.API
dotnet run
```
Access: http://localhost:5000

### Production Deployment
```bash
deploy-production.bat
```

## Environment Configuration

### 1. Database Setup
Update connection strings in `.env` or environment variables:
```
DB_SERVER=10.40.14.22,1433
DB_NAME=OmniChannelDB2
DB_USER=DevSol
DB_PASSWORD=DevvSol1234
```

### 2. Application Insights (Optional)
```
APPINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-key;IngestionEndpoint=https://region.in.applicationinsights.azure.com/
```

## Deployment Options

### Option 1: Windows Service
```bash
# Build and install
deploy-production.bat

# Manual commands
dotnet publish -c Release -o ./publish --self-contained -r win-x64
sc create "KeyLoyalty Service" binPath="C:\path\to\publish\KeyLoyalty.API.exe"
sc start "KeyLoyalty Service"
```

### Option 2: Docker
```bash
# Build image
docker build -t keyloyalty-api .

# Run container
docker run -d -p 5000:5000 \
  -e DB_SERVER="10.40.14.22,1433" \
  -e DB_NAME="OmniChannelDB2" \
  -e DB_USER="DevSol" \
  -e DB_PASSWORD="DevvSol1234" \
  --name loyalty-service keyloyalty-api
```

### Option 3: IIS
```bash
# Publish
dotnet publish -c Release -o ./publish

# Copy to IIS wwwroot
# Configure application pool for .NET 9
# Set environment variables in web.config
```

## Monitoring & Health Checks

### Endpoints
- **Health Check**: `http://localhost:5000/health`
- **API Documentation**: `http://localhost:5000/swagger` (dev only)

### Logging
- **Console**: Real-time logs
- **File**: `./logs/loyalty-YYYY-MM-DD.log`
- **Application Insights**: Telemetry and monitoring

### Database Monitoring
Service polls these databases every 5 minutes:
- OmniChannelDB2 (main transactions)
- AirtimeDataDB (airtime purchases)
- BillsPaymentDB (bill payments)
- KeySwiftDB (money transfers)

## Configuration Files

### appsettings.json (Development)
```json
{
  "ConnectionStrings": {
    "OmniDbConnection": "Server=10.40.14.22,1433;Database=OmniChannelDB2;..."
  }
}
```

### appsettings.Production.json
Uses environment variables for security:
```json
{
  "ConnectionStrings": {
    "OmniDbConnection": "Server=${DB_SERVER};Database=${DB_NAME};..."
  }
}
```

## Security

### Environment Variables
Never hardcode sensitive data. Use:
- `DB_PASSWORD` for database password
- `LOYALTY_API_KEY` for API security

- `APPINSIGHTS_CONNECTION_STRING` for monitoring

### HTTP Configuration
- Development: http://localhost:5000
- Production: http://localhost:5000

## Troubleshooting

### Common Issues
1. **Database Connection**: Check connection string and network access
2. **Port Conflicts**: Modify ports in appsettings.json
3. **Service Won't Start**: Check logs in `./logs/` directory
4. **Port Conflicts**: Modify ports in appsettings.json

### Logs Location
- **Development**: Console output
- **Production**: `./logs/loyalty-YYYY-MM-DD.log`
- **Windows Service**: Event Viewer > Application Logs

### Health Check
```bash
curl http://localhost:5000/health
```
Response: `Healthy` or `Unhealthy` with details

## Performance

### Database Polling
- **Interval**: 5 minutes
- **Timeout**: 30 seconds
- **Retry**: Automatic on failure

### Loyalty Points Calculation
- **Airtime/Data**: 1 point per ₦1
- **NIP Transfer**: 2 points per ₦1  
- **Bill Payment**: 3 points per ₦1

### Tiers
- **Bronze**: 0-500 points
- **Silver**: 501-3,000 points
- **Gold**: 3,001-6,000 points
- **Platinum**: 6,001-10,000 points
- **Diamond**: 10,001+ points

## Support
For issues or questions, check:
1. Application logs
2. Health check endpoint
3. Database connectivity
4. Environment variables configuration