# Government Collections API

A comprehensive government collections system for KeyMobile banking app that enables customers to make payments for taxes, levies, licenses, and statutory fees through multiple payment gateways.

## Features

- **Multi-Gateway Support**: RevPay, Remita, Interswitch, BuyPower
- **Bill Inquiry**: Auto-fetch bill details using reference IDs
- **Payment Processing**: Secure payment processing with real-time confirmation
- **Transaction Management**: Complete transaction lifecycle management
- **Audit Trail**: Comprehensive logging and tracking
- **Caching**: Redis-based caching for performance optimization
- **Security**: JWT authentication and authorization

## Architecture

The solution follows Clean Architecture principles with the following layers:

- **API Layer**: Controllers and middleware
- **Service Layer**: Business logic and external integrations
- **Data Layer**: Repository pattern with MongoDB
- **Domain Layer**: Entities, DTOs, and business rules

## Prerequisites

- .NET 6.0 SDK
- MongoDB
- Redis (optional, for caching)
- Visual Studio 2022 or VS Code

## Configuration

Update `appsettings.json` with your specific settings:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "GovernmentCollections"
  },
  "RevPaySettings": {
    "BaseUrl": "https://xxsg.ebs-rcm.com/interface/",
    "ApiKey": "your-api-key",
    "ClientId": "your-client-id",
    "State": "XXSG"
  }
}
```

## API Endpoints

### Bill Inquiry
```
POST /api/v1/governmentcollections/bill-inquiry
```

### Process Payment
```
POST /api/v1/governmentcollections/process-payment
```

### Verify Payment
```
GET /api/v1/governmentcollections/verify-payment/{transactionReference}
```

### Get User Payments
```
GET /api/v1/governmentcollections/payments?page=1&pageSize=10
```

## Running the Application

1. Clone the repository
2. Navigate to the solution directory
3. Restore packages: `dotnet restore`
4. Update connection strings in appsettings.json
5. Run the application: `dotnet run --project GovernmentCollections.API`

## Docker Support

Build and run using Docker:

```bash
docker build -t government-collections .
docker run -p 8080:80 government-collections
```

## Security Considerations

- All endpoints require JWT authentication
- Input validation using FluentValidation
- Secure API communication with payment gateways
- Comprehensive audit logging
- Rate limiting (recommended for production)

## Performance Optimizations

- Redis caching for frequently accessed data
- Async/await pattern throughout
- Connection pooling for database operations
- Optimized MongoDB queries with indexing

## Monitoring and Logging

- Structured logging with Serilog
- Request/Response logging middleware
- Correlation IDs for request tracking
- Health checks for dependencies

## Testing

Run unit tests:
```bash
dotnet test
```

## Contributing

1. Follow Clean Architecture principles
2. Implement proper error handling
3. Add unit tests for new features
4. Update documentation

## License

Proprietary - Keystone Bank Limited