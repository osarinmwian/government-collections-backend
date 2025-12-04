# Loyalty Event Service - Clean Architecture

Event-driven loyalty service that reads from existing transaction databases for KeyMobile app.

## Architecture Overview

This service reads successful transactions from existing databases and processes them for loyalty points:
- **AirtimeDataDB**: Airtime purchase transactions
- **BillsPaymentDB**: Bill payment transactions  
- **KeySwiftDB**: Money transfer transactions
- **OmniChannelDB2**: Main transaction database

## Architecture Layers

### Domain Layer
- **Events**: Business events (TransactionLoyaltyEvent, AirtimeLoyaltyEvent, BillPaymentLoyaltyEvent)

### Infrastructure Layer
- **Services**: Transaction reading from existing databases
- **Events**: Event publishing and handling
- **Polling**: Background service that polls transaction databases every 5 minutes

### API Layer
- **Controllers**: REST endpoints for loyalty information
- **Program**: Dependency injection setup

## Transaction Processing

The service automatically:
1. Polls transaction databases every 5 minutes
2. Finds successful transactions (Status = 'SUCCESS')
3. Publishes loyalty events for point calculation
4. Processes events in real-time

## Database Configuration

```json
{
  "ConnectionStrings": {
    "OmniDbConnection": "Server=10.40.14.22,1433;Database=OmniChannelDB2;...",
    "AirtimeDbConnection": "Server=10.40.14.22,1433;Database=AirtimeDataDB;...",
    "BillPaymentDbConnection": "Server=10.40.14.22,1433;Database=BillsPaymentDB;...",
    "TransferDbConnection": "Server=10.40.14.22,1433;Database=KeySwiftDB;..."
  }
}
```

## API Security

All API endpoints require authentication using an API key in the request header:

```
X-API-Key: KL-2024-API-KEY-SECURE
```

### API Endpoints

- `GET /api/loyalty/dashboard/{accountNumber}` - Get loyalty dashboard
- `GET /api/loyalty/redeem-options` - Get available redemption options  
- `POST /api/loyalty/redeem-points` - Redeem loyalty points

### Example Usage

```bash
curl -H "X-API-Key: KL-2024-API-KEY-SECURE" \
     http://localhost:5000/api/loyalty/dashboard/1234567890
```

## Running

```bash
cd src/KeyLoyalty.API
dotnet run
```

API will be available at: http://localhost:5000  
Swagger UI: http://localhost:5000/swagger

## Testing Transaction Reading

```bash
dotnet run --project SimpleTransactionReader.csproj
```

This shows successful transactions from all databases that would generate loyalty points.


 var testAccount = "1006817382";
        var testUserId = testAccount;