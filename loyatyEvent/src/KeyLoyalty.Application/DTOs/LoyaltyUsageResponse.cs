namespace KeyLoyalty.Application.DTOs
{
    public class LoyaltyUsageResponse
    {
        public bool UsedLoyaltyPoints { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PointsUsed { get; set; }
        public decimal PointsValue { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public int RemainingPoints { get; set; }
    }
}