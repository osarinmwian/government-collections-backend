using System;
using System.Collections.Generic;

namespace KeyLoyalty.Application.DTOs
{
    public class LoyaltyDashboard
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> AccountNumbers { get; set; } = new();
        public int TotalPoints { get; set; }
        public string Tier { get; set; } = string.Empty;
        public string TierIcon { get; set; } = string.Empty;
        public int PointsToNextTier { get; set; }
        public DateTime PointsExpiryDate { get; set; }
        public List<EarningPoint> EarningPoints { get; set; } = new();
        public List<TierPoint> TierPoints { get; set; } = new();
    }

    public class EarningPoint
    {
        public string Type { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public class TierPoint
    {
        public string Tier { get; set; } = string.Empty;
        public string TierNumber { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RedemptionOption
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class AirtimeRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class BillPaymentRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillerName { get; set; } = string.Empty;
        public string BillType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}