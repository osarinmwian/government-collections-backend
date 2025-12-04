using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using KeyLoyalty.Infrastructure.Services;

namespace OmniChannel.Extensions
{
    // Extension class to add loyalty tracking to existing OmniChannel controllers
    public static class LoyaltyIntegrationExtensions
    {
        public static async Task<bool> ProcessLoyaltyPointsAsync(this ControllerBase controller, 
            string accountNumber, decimal amount, string transactionId, string transactionType, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints) return true;

            var loyaltyTracker = controller.HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
            if (loyaltyTracker == null) return true;

            return await loyaltyTracker.DeductPointsForTransactionAsync(accountNumber, amount, transactionId, transactionType);
        }

        public static async Task ConfirmLoyaltyTransactionAsync(this ControllerBase controller, 
            string transactionId, bool useLoyaltyPoints, bool transactionSuccess, string errorMessage = "")
        {
            if (!useLoyaltyPoints) return;

            var loyaltyTracker = controller.HttpContext.RequestServices.GetService<ILoyaltyTransactionTracker>();
            if (loyaltyTracker == null) return;

            if (transactionSuccess)
                await loyaltyTracker.ConfirmTransactionAsync(transactionId);
            else
                await loyaltyTracker.RollbackTransactionAsync(transactionId, errorMessage);
        }
    }
}

// Sample integration for existing OmniChannel controllers
namespace OmniChannel.Controllers
{
    // Add this to your existing AirtimeController
    public partial class AirtimeController : ControllerBase
    {
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimeRequest request)
        {
            // NEW: Process loyalty points before transaction
            var loyaltyProcessed = await this.ProcessLoyaltyPointsAsync(
                request.AccountNumber, 
                request.Amount, 
                request.TransactionId, 
                "AIRTIME", 
                request.UseLoyaltyPoints ?? false);

            if (!loyaltyProcessed)
            {
                return BadRequest(new { error = "Insufficient loyalty points", code = "LOYALTY_INSUFFICIENT" });
            }

            // EXISTING: Your current airtime processing logic
            var result = await ProcessExistingAirtimeLogic(request);

            // NEW: Confirm or rollback loyalty transaction
            await this.ConfirmLoyaltyTransactionAsync(
                request.TransactionId, 
                request.UseLoyaltyPoints ?? false, 
                result.IsSuccessful, 
                result.ErrorMessage);

            return Ok(result);
        }

        // Your existing method remains unchanged
        private async Task<AirtimeResult> ProcessExistingAirtimeLogic(AirtimeRequest request)
        {
            // Your existing airtime processing code here
            return new AirtimeResult { IsSuccessful = true };
        }
    }

    // Add this to your existing TransferController
    public partial class TransferController : ControllerBase
    {
        [HttpPost("process")]
        public async Task<IActionResult> ProcessTransfer([FromBody] TransferRequest request)
        {
            // NEW: Process loyalty points
            var loyaltyProcessed = await this.ProcessLoyaltyPointsAsync(
                request.DebitAccount, 
                request.Amount, 
                request.TransactionId, 
                "TRANSFER", 
                request.UseLoyaltyPoints ?? false);

            if (!loyaltyProcessed)
            {
                return BadRequest(new { error = "Insufficient loyalty points", code = "LOYALTY_INSUFFICIENT" });
            }

            // EXISTING: Your current transfer processing
            var result = await ProcessExistingTransferLogic(request);

            // NEW: Confirm or rollback
            await this.ConfirmLoyaltyTransactionAsync(
                request.TransactionId, 
                request.UseLoyaltyPoints ?? false, 
                result.IsSuccessful, 
                result.ErrorMessage);

            return Ok(result);
        }

        private async Task<TransferResult> ProcessExistingTransferLogic(TransferRequest request)
        {
            // Your existing transfer processing code here
            return new TransferResult { IsSuccessful = true };
        }
    }

    // Add this to your existing BillPaymentController
    public partial class BillPaymentController : ControllerBase
    {
        [HttpPost("pay")]
        public async Task<IActionResult> PayBill([FromBody] BillPaymentRequest request)
        {
            // NEW: Process loyalty points
            var loyaltyProcessed = await this.ProcessLoyaltyPointsAsync(
                request.AccountNumber, 
                request.Amount, 
                request.TransactionId, 
                "BILL_PAYMENT", 
                request.UseLoyaltyPoints ?? false);

            if (!loyaltyProcessed)
            {
                return BadRequest(new { error = "Insufficient loyalty points", code = "LOYALTY_INSUFFICIENT" });
            }

            // EXISTING: Your current bill payment processing
            var result = await ProcessExistingBillPaymentLogic(request);

            // NEW: Confirm or rollback
            await this.ConfirmLoyaltyTransactionAsync(
                request.TransactionId, 
                request.UseLoyaltyPoints ?? false, 
                result.IsSuccessful, 
                result.ErrorMessage);

            return Ok(result);
        }

        private async Task<BillPaymentResult> ProcessExistingBillPaymentLogic(BillPaymentRequest request)
        {
            // Your existing bill payment processing code here
            return new BillPaymentResult { IsSuccessful = true };
        }
    }
}

// Update your existing DTOs by adding this property
public partial class AirtimeRequest
{
    // Add this to your existing AirtimeRequest class
    public bool? UseLoyaltyPoints { get; set; }
}

public partial class TransferRequest
{
    // Add this to your existing TransferRequest class
    public bool? UseLoyaltyPoints { get; set; }
}

public partial class BillPaymentRequest
{
    // Add this to your existing BillPaymentRequest class
    public bool? UseLoyaltyPoints { get; set; }
}

// Result classes (if you don't have them)
public class AirtimeResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class TransferResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class BillPaymentResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}