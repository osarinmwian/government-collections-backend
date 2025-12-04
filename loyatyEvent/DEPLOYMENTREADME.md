# KeyLoyalty Service - Deployment Guide

## Quick Deployment

### Development
```bash
cd src/KeyLoyalty.API
dotnet run
```
Access: http://localhost:5000

### Production (Windows Service)
```bash
deploy-production.bat
```

### Production (Docker)
```bash
docker build -t keyloyalty .
docker run -d -p 5000:5000 --name loyalty-service keyloyalty
```

## Manual Deployment Steps

### 1. Prerequisites
- .NET 9.0 SDK
- SQL Server access to OmniChannelDB2 (10.40.14.22:1433)
- Windows Server (for service deployment)

### 2. Build Application
```bash
cd src/KeyLoyalty.API
dotnet publish -c Release -o ../../publish --self-contained -r win-x64
```

### 3. Configure Environment
```bash
# Set database connection
setx DB_SERVER "10.40.14.22,1433" /M
setx DB_NAME "OmniChannelDB2" /M
setx DB_USER "DevSol" /M
setx DB_PASSWORD "DevvSol1234" /M
setx ASPNETCORE_ENVIRONMENT "Production" /M
```

### 4. Install Windows Service
```bash
sc create "KeyLoyalty Service" binPath="C:\path\to\publish\KeyLoyalty.API.exe" start=auto
sc start "KeyLoyalty Service"
```

### 5. Verify Deployment
```bash
# Health check
curl http://localhost:5000/health

# Test API
curl http://localhost:5000/api/loyalty/dashboard/1006817382
```

## Deployment Options

### Option A: Windows Service (Recommended)
- **Use**: Production servers
- **Benefits**: Auto-start, system integration
- **Command**: `deploy-production.bat`

### Option B: Docker Container
- **Use**: Containerized environments
- **Benefits**: Isolation, portability
- **Command**: `docker build -t keyloyalty .`

### Option C: IIS Hosting
- **Use**: Web server environments
- **Benefits**: Web management, load balancing
- **Setup**: Copy publish folder to IIS wwwroot

### Option D: Console Application
- **Use**: Development, testing
- **Benefits**: Direct control, debugging
- **Command**: `dotnet run`

## Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "OmniDbConnection": "Server=10.40.14.22,1433;Database=OmniChannelDB2;User Id=DevSol;password=DevvSol1234;TrustServerCertificate=True;Connection Timeout=30;"
  }
}
```

### Environment Variables
```bash
DB_SERVER=10.40.14.22,1433
DB_NAME=OmniChannelDB2
DB_USER=DevSol
DB_PASSWORD=DevvSol1234
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

## Monitoring

### Health Endpoints
- **Health Check**: http://localhost:5000/health
- **Swagger UI**: http://localhost:5000/swagger (dev only)

### Logs
- **Location**: `./logs/keyloyalty-YYYY-MM-DD.log`
- **Format**: Structured JSON logging
- **Retention**: 7 days

### Key Metrics
- Transaction processing rate: ~100/minute
- Database polling: Every 5 minutes
- Memory usage: <100MB
- Response time: <500ms

## Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check logs
type logs\keyloyalty-*.log

# Verify database connection
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -Q "SELECT 1"

# Check service status
sc query "KeyLoyalty Service"
```

#### Database Connection Failed
```bash
# Test connectivity
telnet 10.40.14.22 1433

# Verify credentials
sqlcmd -S 10.40.14.22,1433 -U DevSol -P DevvSol1234 -d OmniChannelDB2 -Q "SELECT COUNT(*) FROM KeystoneOmniTransactions"
```

#### Port Already in Use
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill process (replace PID)
taskkill /PID 1234 /F

# Or change port in appsettings.json
"Urls": "http://localhost:5001"
```

### Performance Issues
```bash
# Check memory usage
tasklist /FI "IMAGENAME eq KeyLoyalty.API.exe"

# Monitor database queries
# Check logs for slow query warnings

# Verify polling frequency
# Look for "Starting transaction polling" in logs every 5 minutes
```

## Security

### Database Security
- Use environment variables for passwords
- Enable SQL Server encryption
- Restrict database user permissions

### API Security
- Configure API keys in production
- Enable HTTPS in production
- Implement rate limiting

### Network Security
- Firewall rules for port 5000
- VPN access for database server
- Monitor access logs

## Backup & Recovery

### Database Backup
```sql
-- Backup loyalty data
BACKUP DATABASE OmniChannelDB2 TO DISK = 'C:\Backup\OmniChannelDB2.bak'
```

### Application Backup
```bash
# Backup published application
xcopy /E /I publish backup\keyloyalty-backup-%DATE%

# Backup configuration
copy appsettings.json backup\
copy logs\*.log backup\logs\
```

### Recovery Steps
1. Stop service: `sc stop "KeyLoyalty Service"`
2. Restore application files
3. Update configuration
4. Start service: `sc start "KeyLoyalty Service"`
5. Verify health check

## Production Checklist

### Pre-Deployment
- [ ] Database connectivity tested
- [ ] Environment variables configured
- [ ] SSL certificates installed (if HTTPS)
- [ ] Firewall rules configured
- [ ] Backup procedures tested

### Post-Deployment
- [ ] Health check returns "Healthy"
- [ ] Service starts automatically
- [ ] Logs are being written
- [ ] Transaction polling is active
- [ ] API endpoints respond correctly
- [ ] Database queries execute successfully

### Monitoring Setup
- [ ] Log monitoring configured
- [ ] Performance counters enabled
- [ ] Alert thresholds set
- [ ] Backup schedules active
- [ ] Health check monitoring

## Support Commands

### Service Management
```bash
# Start service
sc start "KeyLoyalty Service"

# Stop service
sc stop "KeyLoyalty Service"

# Restart service
sc stop "KeyLoyalty Service" && sc start "KeyLoyalty Service"

# Delete service
sc delete "KeyLoyalty Service"

# Check service status
sc query "KeyLoyalty Service"
```

### Log Analysis
```bash
# View recent logs
type logs\keyloyalty-*.log | findstr "ERROR"

# Count transactions processed today
type logs\keyloyalty-*.log | findstr "Published.*loyalty event" | find /c /v ""

# Check database connections
type logs\keyloyalty-*.log | findstr "Database"
```

### Health Verification
```bash
# Complete health check
curl http://localhost:5000/health && echo Service is healthy

# Test loyalty API
curl http://localhost:5000/api/loyalty/dashboard/1006817382

# Check transaction processing
curl -X POST http://localhost:5000/api/loyalty/process-transaction -H "Content-Type: application/json" -d "{\"transactionId\":\"TEST-001\",\"accountNumber\":\"1006817382\",\"transactionType\":\"NIP_TRANSFER\",\"amount\":1000,\"username\":\"testuser\"}"
```