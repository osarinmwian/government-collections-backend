namespace KeyLoyalty.Application.DTOs
{
    public class LoyaltyTransactionRequest
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty; // TRANSFER, AIRTIME, BILL_PAYMENT
        public bool UseLoyaltyPoints { get; set; }
        public int? LoyaltyPointsToUse { get; set; }
        public string? BeneficiaryAccount { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BillerCode { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
    }

    public class LoyaltyTransactionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal ActualAmount { get; set; }
        public int PointsUsed { get; set; }
        public decimal PointsValue { get; set; }
        public string? PendingRedemptionId { get; set; }
    }
}