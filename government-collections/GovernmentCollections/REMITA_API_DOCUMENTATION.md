# Remita API Integration Documentation

## Overview
This document describes the complete Remita API integration implementation that follows the official Remita API workflow for bill payments and collections.

## API Workflow
The implementation follows this sequence:
1. **Authentication** - Get access token
2. **Get Billers** - Retrieve available billers
3. **Get Biller Products** - Get products for a specific biller
4. **Customer Validation** - Validate customer details (optional)
5. **Transaction Initiation** - Create RRR for payment
6. **RRR Lookup** - Get transaction details
7. **Process Payment** - Process the actual payment
8. **Query Transaction** - Check payment status

## Configuration
Add these settings to your `appsettings.json`:

```json
{
  "Remita": {
    "BaseUrl": "https://demo.remita.net",
    "Username": "6O0GI4EWOU010F9J",
    "Password": "UGBNABRSWKLQL1GGIV5SLW8AO2L3INE7",
    "PublicKey": "your-public-key-here",
    "SecretKey": "your-secret-key-here"
  }
}
```

## API Endpoints

### 1. Authentication (Internal)
- **Purpose**: Get access token for API calls
- **Method**: Handled automatically by `RemitaAuthService`
- **Caching**: Tokens are cached and auto-refreshed

### 2. Get Billers
- **Endpoint**: `GET /api/remita/billers`
- **Purpose**: Retrieve all available billers
- **Authentication**: Bearer token (auto-handled)
- **Response**: List of billers with categories

### 3. Get Biller Products
- **Endpoint**: `GET /api/remita/biller/{billerId}/products`
- **Purpose**: Get products/services for a specific biller
- **Parameters**: 
  - `billerId` (path): The biller ID
- **Authentication**: Bearer token (auto-handled)

### 4. Customer Validation
- **Endpoint**: `POST /api/remita/customer/validate`
- **Purpose**: Validate customer details before payment
- **Request Body**:
```json
{
  "billPaymentProductId": "41958636",
  "customerId": "customer@email.com"
}
```

### 5. Transaction Initiation
- **Endpoint**: `POST /api/remita/transaction/initiate`
- **Purpose**: Create RRR for payment
- **Request Body**:
```json
{
  "billPaymentProductId": "41958636",
  "amount": 2000.00,
  "transactionRef": "unique-transaction-ref",
  "name": "Customer Name",
  "email": "customer@email.com",
  "phoneNumber": "080123456789",
  "customerId": "customer@email.com",
  "metadata": {
    "customFields": [
      {
        "variableName": "size",
        "value": "40abc"
      }
    ]
  }
}
```

### 6. RRR Lookup
- **Endpoint**: `GET /api/remita/transaction/lookup/{rrr}`
- **Purpose**: Get transaction details by RRR
- **Parameters**: 
  - `rrr` (path): The Remita Retrieval Reference

### 7. Process Payment
- **Endpoint**: `POST /api/remita/transaction/pay`
- **Purpose**: Process payment for an RRR
- **Request Body**:
```json
{
  "rrr": "170799159272",
  "transactionRef": "unique-payment-ref",
  "amount": 2000,
  "channel": "internetbanking",
  "metadata": {
    "fundingSource": "HERITAGE",
    "payerAccountNumber": "2035468030"
  }
}
```

### 8. Query Transaction
- **Endpoint**: `GET /api/remita/transaction/query/{transactionRef}`
- **Purpose**: Check payment status
- **Parameters**: 
  - `transactionRef` (path): The transaction reference

### 9. Get Active Banks
- **Endpoint**: `GET /api/remita/banks`
- **Purpose**: Get list of active banks for payments

## Response Format
All Remita API responses follow this format:
```json
{
  "status": "00",
  "message": "Request processed successfully",
  "data": { ... }
}
```

Status codes:
- `"00"` - Success
- `"23"` - Already processed transaction
- `"94"` - Duplicate transaction
- `"99"` - Error/Failure

## Error Handling
- All endpoints include comprehensive error handling
- Failed requests return appropriate HTTP status codes
- Detailed error logging for troubleshooting
- Automatic token refresh on authentication failures

## Security Features
- Bearer token authentication for all API calls
- Automatic token caching and refresh
- Secure credential handling
- Request/response logging (with credential masking)

## Usage Example

### Complete Payment Flow
```csharp
// 1. Get available billers
var billers = await httpClient.GetAsync("/api/remita/billers");

// 2. Get products for a biller
var products = await httpClient.GetAsync("/api/remita/biller/QADEMO/products");

// 3. Validate customer (optional)
var validation = await httpClient.PostAsync("/api/remita/customer/validate", 
    new { billPaymentProductId = "41958636", customerId = "customer@email.com" });

// 4. Initiate transaction
var initiate = await httpClient.PostAsync("/api/remita/transaction/initiate", 
    new { 
        billPaymentProductId = "41958636",
        amount = 2000.00,
        transactionRef = Guid.NewGuid().ToString(),
        name = "John Doe",
        email = "john@email.com",
        phoneNumber = "08012345678",
        customerId = "john@email.com"
    });

// 5. Get RRR from response and lookup details
var rrr = "170799159272"; // from initiate response
var lookup = await httpClient.GetAsync($"/api/remita/transaction/lookup/{rrr}");

// 6. Process payment
var payment = await httpClient.PostAsync("/api/remita/transaction/pay",
    new {
        rrr = rrr,
        transactionRef = Guid.NewGuid().ToString(),
        amount = 2000,
        channel = "internetbanking",
        metadata = new { fundingSource = "HERITAGE" }
    });

// 7. Query transaction status
var status = await httpClient.GetAsync($"/api/remita/transaction/query/{transactionRef}");
```

## Notes
- All requests require proper authentication (handled automatically)
- Content-Type must be `application/json` for POST requests
- Transaction references must be unique
- RRR (Remita Retrieval Reference) is generated after transaction initiation
- Payment processing requires a valid RRR
- Always verify payment status after processing