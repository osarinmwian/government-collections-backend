# OmniChannel Loyalty Integration - Step by Step

## Step 1: Add Service Registration

In your **OmniChannel Program.cs**, add these services:

```csharp
// Add these lines to your existing Program.cs
builder.Services.AddScoped<ILoyaltyTransactionTracker, LoyaltyTransactionTracker>();
builder.Services.AddScoped<ICustomerLoyaltyRepository, CustomerLoyaltyRepository>();
builder.Services.AddScoped<IAccountMappingService, AccountMappingService>();

builder.Services.AddHttpClient<ILoyaltyTransactionTracker>(client =>
{
    client.BaseAddress = new Uri("https://your-notification-service.com");
});
```

## Step 2: Update Your Request DTOs

Add `UseLoyaltyPoints` property to your existing request classes:

```csharp
// In your existing AirtimeRequest class
public class AirtimeRequest
{
    // Your existing properties...
    public string AccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; }
    
    // ADD THIS LINE
    public bool? UseLoyaltyPoints { get; set; }
}

// Do the same for TransferRequest and BillPaymentRequest
```

## Step 3: Update Your Controllers

### Option A: Use Extension Methods (Recommended)

Add the extension class to your project and update your controllers:

```csharp
[HttpPost("airtime")]
public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimeRequest request)
{
    // ADD: Check loyalty points before processing
    var loyaltyProcessed = await this.ProcessLoyaltyPointsAsync(
        request.AccountNumber, request.Amount, request.TransactionId, 
        "AIRTIME", request.UseLoyaltyPoints ?? false);

    if (!loyaltyProcessed)
        return BadRequest("Insufficient loyalty points");

    // YOUR EXISTING CODE STAYS THE SAME
    var result = await YourExistingAirtimeMethod(request);

    // ADD: Confirm or rollback loyalty transaction
    await this.ConfirmLoyaltyTransactionAsync(request.TransactionId, 
        request.UseLoyaltyPoints ?? false, result.IsSuccessful, result.ErrorMessage);

    return Ok(result);
}
```

### Option B: Direct Integration

```csharp
[HttpPost("airtime")]
public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimeRequest request)
{
    // ADD: Loyalty point processing
    if (request.UseLoyaltyPoints == true)
    {
        var loyaltyTracker = HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
        if (loyaltyTracker != null)
        {
            var pointsDeducted = await loyaltyTracker.DeductPointsForTransactionAsync(
                request.AccountNumber, request.Amount, request.TransactionId, "AIRTIME");
            
            if (!pointsDeducted)
                return BadRequest("Insufficient loyalty points");
        }
    }

    // YOUR EXISTING AIRTIME PROCESSING CODE
    var result = await ProcessAirtime(request);

    // ADD: Confirm or rollback
    if (request.UseLoyaltyPoints == true)
    {
        var loyaltyTracker = HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
        if (loyaltyTracker != null)
        {
            if (result.IsSuccessful)
                await loyaltyTracker.ConfirmTransactionAsync(request.TransactionId);
            else
                await loyaltyTracker.RollbackTransactionAsync(request.TransactionId, result.ErrorMessage);
        }
    }

    return Ok(result);
}
```

## Step 4: Database Setup

Run the SQL script to create the tracking table:

```sql
-- Run this in your OmniChannel database
CREATE TABLE LoyaltyTransactionTracker (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionId NVARCHAR(100) NOT NULL UNIQUE,
    AccountNumber NVARCHAR(20) NOT NULL,
    PointsUsed INT NOT NULL,
    AmountUsed DECIMAL(18,2) NOT NULL,
    TransactionType NVARCHAR(50) NOT NULL,
    OriginalPoints INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    RollbackReason NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
```

## Step 5: Test Integration

1. **Test Normal Flow**: Existing transactions should work unchanged
2. **Test Loyalty Flow**: Send `"useLoyaltyPoints": true` in request
3. **Test Insufficient Points**: Verify proper error handling
4. **Test Rollback**: Verify points restored on transaction failure

## Key Benefits

✅ **Non-Breaking**: Existing code continues to work  
✅ **Optional**: Loyalty points only used when requested  
✅ **Automatic**: Points deducted/restored automatically  
✅ **Notifications**: Email and SMS sent automatically  
✅ **Tracking**: All loyalty transactions logged  

## Mobile App Integration

The mobile app can now:
1. Show user's `totalCashValue` from loyalty dashboard
2. Add toggle for "Use Loyalty Points" in transaction screens
3. Send `"useLoyaltyPoints": true` in transaction requests
4. Handle "Insufficient loyalty points" error responses

## Error Handling

The system returns specific error codes:
- `LOYALTY_INSUFFICIENT`: Not enough points
- Standard transaction errors remain unchanged

This ensures your existing error handling continues to work while adding loyalty-specific errors.