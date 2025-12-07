# Remita Service Structure

This folder contains the restructured Remita service implementation, organized into logical components for better maintainability and separation of concerns.

## Folder Structure

```
Services/Remita/
├── Authentication/
│   ├── IRemitaAuthenticationService.cs
│   └── RemitaAuthenticationService.cs
├── BillPayment/
│   ├── IRemitaBillPaymentService.cs
│   └── RemitaBillPaymentService.cs
├── Payment/
│   ├── IRemitaPaymentService.cs
│   └── RemitaPaymentService.cs
├── Transaction/
│   ├── IRemitaTransactionService.cs
│   └── RemitaTransactionService.cs
├── Invoice/
│   ├── IRemitaInvoiceService.cs
│   └── RemitaInvoiceService.cs
├── Gateway/
│   ├── IRemitaPaymentGatewayService.cs
│   └── RemitaPaymentGatewayService.cs
├── Validation/
│   ├── IPinValidationService.cs
│   └── PinValidationService.cs
├── IRemitaService.cs
├── RemitaService.cs
├── RemitaServiceExtensions.cs
└── README.md
```

## Components

### Authentication Service
- Handles token management and authentication with Remita API
- Manages access token lifecycle and authorization headers

### Bill Payment Service
- Manages biller operations (get billers, biller details)
- Handles customer validation for bill payments

### Payment Service
- Processes various payment operations
- Handles RRR payments, payment verification, and bank operations
- Integrates with settlement service for payment processing

### Transaction Service
- Manages transaction processing with authentication
- Handles transaction status queries
- Integrates PIN validation and 2FA

### Invoice Service
- Handles invoice generation and payment status verification
- Manages hash generation for secure transactions

### Gateway Service
- Manages payment gateway operations
- Handles transaction verification and checkout hash generation

### Validation Service
- Handles PIN validation and 2FA authentication
- Manages user authentication for secure transactions

### Main Service (Facade)
- Acts as a facade that delegates to specialized services
- Maintains the original interface for backward compatibility
- Clean, minimal implementation with single-line method delegations

## Usage

Register all services using the extension method:

```csharp
services.AddRemitaServices();
```

This will register all the required services with their interfaces for dependency injection.

## Benefits

1. **Separation of Concerns**: Each service handles a specific domain
2. **Maintainability**: Easier to locate and modify specific functionality
3. **Testability**: Individual components can be unit tested in isolation
4. **Reusability**: Services can be used independently if needed
5. **Single Responsibility**: Each class has a clear, focused purpose
6. **Clean Architecture**: Facade pattern provides a simple interface to complex subsystems