namespace KeyLoyalty.Application.DTOs
{
    public class RedeemPointsRequest
    {
        public string Username { get; set; } = string.Empty;
        public string RedemptionOptionId { get; set; } = string.Empty;
        public int PointsToRedeem { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string RedemptionType { get; set; } = string.Empty;
    }

    public class RedemptionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal AmountRedeemed { get; set; }
        public int RemainingPoints { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string RedemptionId { get; set; } = string.Empty;
    }

    public class TransactionConfirmationRequest
    {
        public string RedemptionId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }

    public class TransactionConfirmationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool PointsRolledBack { get; set; }
    }


}