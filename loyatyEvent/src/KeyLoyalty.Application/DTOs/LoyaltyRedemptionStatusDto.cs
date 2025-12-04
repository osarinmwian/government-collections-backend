using System;

namespace KeyLoyalty.Application.DTOs
{
    public class LoyaltyRedemptionStatusDto
    {
        public string UserIdOrAccount { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public bool IsCredited { get; set; }
        public decimal CreditAmount { get; set; }
        public int LoyaltyPointsUsed { get; set; }
        public decimal LoyaltyAmountValue { get; set; }
        public int PreviousPoints { get; set; }
        public int CurrentPoints { get; set; }
        public decimal TotalTransactionAmount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}