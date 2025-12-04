namespace KeyLoyalty.Application.DTOs
{
    public class TransferCompletedRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string DebitAccount { get; set; } = string.Empty;
        public string CreditAccount { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class AirtimeCompletedRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class BillPaymentCompletedRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillerName { get; set; } = string.Empty;
        public string BillType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}