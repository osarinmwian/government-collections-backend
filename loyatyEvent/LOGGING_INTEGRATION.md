# Loyalty System Logging Integration

## Overview
This implementation adds comprehensive inbound and outbound logging to the KeyLoyalty system that can be accessed from the OmniChannel system.

## Features Added

### 1. Loyalty Log Service (`ILoyaltyLogService`)
- **Location**: `KeyLoyalty.Infrastructure/Services/LoyaltyLogService.cs`
- **Purpose**: Manages in-memory log storage and retrieval
- **Capabilities**:
  - Store inbound requests (method, path, headers, body)
  - Store outbound responses (status code, headers, body)
  - Retrieve logs by direction (inbound/outbound/all)
  - Filter logs by date range
  - Correlation ID tracking

### 2. Logs API Controller
- **Location**: `KeyLoyalty.API/Controllers/LogsController.cs`
- **Endpoints**:
  - `GET /api/logs/inbound` - Get inbound request logs
  - `GET /api/logs/outbound` - Get outbound response logs
  - `GET /api/logs/all` - Get all logs
  - `POST /api/logs/test` - Test logging functionality

### 3. Enhanced Request/Response Middleware
- **Location**: `KeyLoyalty.API/Middleware/RequestResponseLoggingMiddleware.cs`
- **Features**:
  - Automatic correlation ID generation
  - Integration with LoyaltyLogService
  - Structured logging with Serilog

### 4. OmniChannel Integration
- **Controller**: `OmniChannel.API/Controllers/KeyLoyaltyController.cs`
- **Service**: `OmniChannel.Gateway/KeyLoyaltyService.cs`
- **New Endpoints**:
  - `GET /KeyLoyalty/Logs/Inbound`
  - `GET /KeyLoyalty/Logs/Outbound`
  - `GET /KeyLoyalty/Logs/All`

## Usage Examples

### From OmniChannel API

#### Get Recent Inbound Logs
```http
GET /KeyLoyalty/Logs/Inbound?limit=50
```

#### Get Logs for Specific Date Range
```http
GET /KeyLoyalty/Logs/All?fromDate=2024-01-01T00:00:00&toDate=2024-01-02T00:00:00&limit=100
```

### Direct Loyalty API Access

#### Get Outbound Logs
```http
GET http://localhost:5000/api/logs/outbound?limit=25
```

#### Test Logging
```http
POST http://localhost:5000/api/logs/test
Content-Type: application/json

{
  "testMessage": "This is a test log entry",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Log Entry Structure

```json
{
  "id": "guid",
  "timestamp": "2024-01-01T12:00:00Z",
  "direction": "INBOUND|OUTBOUND",
  "method": "GET|POST|PUT|DELETE",
  "path": "/api/loyalty/points/user123",
  "statusCode": 200,
  "headers": {
    "Content-Type": "application/json",
    "X-API-Key": "***"
  },
  "body": { "data": "..." },
  "correlationId": "correlation-guid",
  "source": "KeyLoyalty"
}
```

## Configuration

### Loyalty System (appsettings.json)
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/inbound-transactions-.log",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/outbound-transactions-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### OmniChannel System (Web.config)
```xml
<appSettings>
  <add key="KeyLoyaltyBaseUrl" value="http://localhost:5000/" />
  <add key="KeyLoyaltyApiKey" value="your-api-key-here" />
</appSettings>
```

## Memory Management
- Logs are stored in-memory with a maximum of 10,000 entries
- Older entries are automatically removed when limit is exceeded
- For production, consider implementing database storage or external logging service

## Security Considerations
- API keys and sensitive headers are logged (consider masking in production)
- Logs contain request/response bodies (ensure compliance with data protection regulations)
- Access to log endpoints should be restricted in production environments

## Monitoring and Troubleshooting
- All logging operations are also written to Serilog for persistent storage
- Correlation IDs help track requests across system boundaries
- Error handling ensures logging failures don't break main application flow