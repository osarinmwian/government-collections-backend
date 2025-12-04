using System;

namespace KeyLoyalty.Domain.Events
{
    public abstract class BaseEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
    }

    public class TransactionKeyLoyaltyEvent : BaseEvent
    {
        public TransactionKeyLoyaltyEvent()
        {
            EventType = nameof(TransactionKeyLoyaltyEvent);
        }

        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class AirtimeKeyLoyaltyEvent : BaseEvent
    {
        public AirtimeKeyLoyaltyEvent()
        {
            EventType = nameof(AirtimeKeyLoyaltyEvent);
        }

        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class BillPaymentKeyLoyaltyEvent : BaseEvent
    {
        public BillPaymentKeyLoyaltyEvent()
        {
            EventType = nameof(BillPaymentKeyLoyaltyEvent);
        }

        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillerName { get; set; } = string.Empty;
        public string BillType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}