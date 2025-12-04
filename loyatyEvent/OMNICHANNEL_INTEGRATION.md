# OmniChannel Loyalty Integration Guide

## 1. Register Service in OmniChannel Program.cs

```csharp
// Add to OmniChannel Program.cs
builder.Services.AddScoped<ILoyaltyTransactionTracker, LoyaltyTransactionTracker>();
builder.Services.AddHttpClient<ILoyaltyTransactionTracker>();
```

## 2. Update Transaction Request DTOs

Add `UseLoyaltyPoints` field to your existing request DTOs:

```csharp
public class AirtimeRequest
{
    // Existing fields...
    public bool UseLoyaltyPoints { get; set; }
}

public class TransferRequest  
{
    // Existing fields...
    public bool UseLoyaltyPoints { get; set; }
}

public class BillPaymentRequest
{
    // Existing fields...
    public bool UseLoyaltyPoints { get; set; }
}
```

## 3. Integration in Controllers

### Airtime Controller
```csharp
[HttpPost("airtime")]
public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimeRequest request)
{
    var loyaltyTracker = HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
    
    // Deduct loyalty points if requested
    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        var pointsDeducted = await loyaltyTracker.DeductPointsForTransactionAsync(
            request.AccountNumber, request.Amount, request.TransactionId, "AIRTIME");
        
        if (!pointsDeducted)
            return BadRequest("Insufficient loyalty points");
    }

    // Process airtime transaction...
    var result = await ProcessAirtimeTransaction(request);

    // Confirm or rollback loyalty points
    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        if (result.IsSuccessful)
            await loyaltyTracker.ConfirmTransactionAsync(request.TransactionId);
        else
            await loyaltyTracker.RollbackTransactionAsync(request.TransactionId, result.ErrorMessage);
    }

    return Ok(result);
}
```

### Transfer Controller
```csharp
[HttpPost("transfer")]
public async Task<IActionResult> ProcessTransfer([FromBody] TransferRequest request)
{
    var loyaltyTracker = HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
    
    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        var pointsDeducted = await loyaltyTracker.DeductPointsForTransactionAsync(
            request.AccountNumber, request.Amount, request.TransactionId, "TRANSFER");
        
        if (!pointsDeducted)
            return BadRequest("Insufficient loyalty points");
    }

    var result = await ProcessTransferTransaction(request);

    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        if (result.IsSuccessful)
            await loyaltyTracker.ConfirmTransactionAsync(request.TransactionId);
        else
            await loyaltyTracker.RollbackTransactionAsync(request.TransactionId, result.ErrorMessage);
    }

    return Ok(result);
}
```

### Bill Payment Controller
```csharp
[HttpPost("billpayment")]
public async Task<IActionResult> PayBill([FromBody] BillPaymentRequest request)
{
    var loyaltyTracker = HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
    
    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        var pointsDeducted = await loyaltyTracker.DeductPointsForTransactionAsync(
            request.AccountNumber, request.Amount, request.TransactionId, "BILL_PAYMENT");
        
        if (!pointsDeducted)
            return BadRequest("Insufficient loyalty points");
    }

    var result = await ProcessBillPaymentTransaction(request);

    if (request.UseLoyaltyPoints && loyaltyTracker != null)
    {
        if (result.IsSuccessful)
            await loyaltyTracker.ConfirmTransactionAsync(request.TransactionId);
        else
            await loyaltyTracker.RollbackTransactionAsync(request.TransactionId, result.ErrorMessage);
    }

    return Ok(result);
}
```

## 4. Email Notification Setup

The system automatically sends emails when transactions complete. Configure your email service endpoint in appsettings.json:

```json
{
  "HttpClient": {
    "BaseAddress": "https://your-notification-service.com"
  }
}
```

## Features

- **Automatic Point Deduction**: Points deducted when transaction starts
- **Rollback on Failure**: Points restored if transaction fails  
- **Email Notifications**: Users notified of successful/failed transactions
- **Transaction Tracking**: All loyalty point usage tracked in database
- **No New Endpoints**: Integrates with existing transaction flows