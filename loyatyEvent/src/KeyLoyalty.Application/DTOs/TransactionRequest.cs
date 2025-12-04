namespace KeyLoyalty.Application.DTOs;

public class TransactionRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BulkTransactionRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public List<TransactionItem> Transactions { get; set; } = new();
}

public class TransactionItem
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TransactionProcessRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "AIRTIME_PURCHASE", "BILL_PAYMENT", "NIP_TRANSFER"
}