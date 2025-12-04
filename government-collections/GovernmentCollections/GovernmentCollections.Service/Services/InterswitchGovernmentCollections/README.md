# Interswitch Service Refactoring

## Overview
The Interswitch service has been refactored into separate, focused services for better maintainability and optimization based on the exact API flow provided.

## Service Structure

### 1. InterswitchAuthService
**Purpose**: Handles authentication and token management
- `AuthenticateAsync()` - Authenticates with Interswitch OAuth
- `IsTokenValidAsync()` - Checks if cached token is valid
- `GetValidTokenAsync()` - Returns valid token (cached or new)

### 2. InterswitchServicesService
**Purpose**: Handles service discovery and biller management
- `GetGovernmentBillersAsync()` - Gets all government billers (Flow #1)
- `GetBillersByCategoryAsync(categoryId)` - Gets billers by category (Flow #2)
- `GetGovernmentCategoriesAsync()` - Gets service categories (Flow #6)
- `GetServiceOptionsAsync(serviceId)` - Gets service options (Flow #3)

### 3. InterswitchTransactionService
**Purpose**: Handles transaction operations
- `ProcessPaymentAsync(request)` - Processes payments (Flow #4)
- `ValidateCustomersAsync(request)` - Validates customers (Flow #5)
- `VerifyTransactionAsync(requestReference)` - Verifies transactions (Flow #7)
- `GetTransactionHistoryAsync(userId, page, pageSize)` - Gets transaction history

### 4. InterswitchService (Main Service)
**Purpose**: Orchestrates all operations through the specialized services
- Acts as a facade pattern
- Delegates calls to appropriate specialized services
- Maintains the original interface for backward compatibility

## API Flow Implementation

### Flow #1: Get Services
```
GET {ServicesUrl}/quicktellerservice/api/v5/services
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
```

### Flow #2: Get Services by Category
```
GET {ServicesUrl}/quicktellerservice/api/v5/services?categoryId={id}
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
```

### Flow #3: Get Service Options
```
GET {ServicesUrl}/quicktellerservice/api/v5/services/options?serviceid={billerid}
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
```

### Flow #4: Process Transaction
```
POST {ServicesUrl}/quicktellerservice/api/v5/Transactions
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
Body: {"TerminalId":"3PBL0001","paymentCode":"0488051528","customerId":"2348124888436","customerMobile":"2348124888436","customerEmail":"johndoe@gmail.com","amount":"10000","requestReference":"122200898163"}
```

### Flow #5: Validate Customers
```
POST {ServicesUrl}/quicktellerservice/api/v5/Transactions/validatecustomers
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
Body: {"customers":[{"PaymentCode":"0488051528","CustomerId":"08124888436"}],"TerminalId":"3pbl"}
```

### Flow #6: Get Categories
```
GET {ServicesUrl}/quicktellerservice/api/v5/services/categories
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
```

### Flow #7: Verify Transaction
```
GET {ServicesUrl}/quicktellerservice/api/v5/Transactions?requestRef={requestReferencevalue}
Headers: Authorization: Bearer {token}, Content-Type: application/json, TerminalID: {terminalId}
```

## Key Updates

1. **URL Updates**: All URLs now match the exact flow provided
2. **Header Standardization**: All requests include proper headers (Authorization, Content-Type, TerminalID)
3. **Separation of Concerns**: Each service handles specific functionality
4. **Improved Maintainability**: Easier to test, debug, and extend individual services
5. **Dependency Injection**: Proper service registration with `InterswitchServiceExtensions`

## Usage

Register services in your DI container:
```csharp
services.AddInterswitchServices(configuration);
```

Inject and use the main service:
```csharp
public class MyController
{
    private readonly IInterswitchService _interswitchService;
    
    public MyController(IInterswitchService interswitchService)
    {
        _interswitchService = interswitchService;
    }
}
```

## Benefits

- **Modularity**: Each service can be tested and maintained independently
- **Performance**: Better caching and resource management
- **Scalability**: Services can be scaled independently if needed
- **Readability**: Cleaner, more focused code
- **Compliance**: Exact implementation of the provided API flow