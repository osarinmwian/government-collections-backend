namespace KeyLoyalty.Application.DTOs
{
    public class TransactionStatusResponse
    {
        public string TransactionReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // PENDING, PROCESSING, CONFIRMED
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool UsedLoyaltyPoints { get; set; }
        public int PointsDeducted { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
    }
}