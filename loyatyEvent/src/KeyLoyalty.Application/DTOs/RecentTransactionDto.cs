namespace KeyLoyalty.Application.DTOs
{
    public class RecentTransactionDto
    {
        public string TransactionReference { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public bool UsedLoyaltyPoints { get; set; }
        public int PointsUsed { get; set; }
    }
}