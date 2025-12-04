using System;

namespace KeyLoyalty.Domain.Entities
{
    public enum LoyaltyTier
    {
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Diamond = 4,
        Platinum = 5
    }

    public enum TransactionType
    {
        TRANSFER,
        AIRTIME,
        BILL_PAYMENT,
        DEPOSIT,
        WITHDRAWAL
    }

    public enum RedemptionType
    {
        CASHBACK,
        DISCOUNT,
        VOUCHER,
        TRANSFER
    }

    public class CustomerLoyalty
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int LifetimePoints { get; set; }
        public LoyaltyTier Tier { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime PointsExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1);
        public List<string> AccountNumbers { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public decimal TransactionVolume { get; set; }
        public int TransactionCount { get; set; }
    }

    public class PointTransaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public int Points { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }

    public class PointRedemption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public int PointsRedeemed { get; set; }
        public decimal AmountRedeemed { get; set; }
        public RedemptionType RedemptionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string TransactionId { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; } = true;
    }

    public class LoyaltyAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty; // EARNING, REDEMPTION, EXPIRY, TIER_UPGRADE
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}