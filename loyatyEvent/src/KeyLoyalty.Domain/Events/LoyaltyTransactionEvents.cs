using System;

namespace KeyLoyalty.Domain.Events
{
    public abstract class LoyaltyTransactionEvent : BaseEvent
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Username { get; set; } = string.Empty;
        public new DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class LoyaltyTransferEvent : LoyaltyTransactionEvent
    {
        public string TransactionType { get; set; } = "NIP_TRANSFER";
        public string DebitAccount { get; set; } = string.Empty;
        public string CreditAccount { get; set; } = string.Empty;
    }

    public class LoyaltyAirtimeEvent : LoyaltyTransactionEvent
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string TransactionType { get; set; } = "AIRTIME";
    }

    public class LoyaltyBillPaymentEvent : LoyaltyTransactionEvent
    {
        public string BillerName { get; set; } = string.Empty;
        public string BillType { get; set; } = string.Empty;
        public string TransactionType { get; set; } = "BILL_PAYMENT";
    }
}