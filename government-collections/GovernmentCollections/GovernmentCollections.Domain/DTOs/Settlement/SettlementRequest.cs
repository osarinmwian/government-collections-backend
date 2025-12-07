using System.Text.Json.Serialization;

namespace GovernmentCollections.Domain.DTOs.Settlement;

public class DebitRequest
{
    public string TransactionRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DebitAccount { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, decimal>? Commissions { get; set; }
    
    public string CommissionCode { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string T24TransactionType { get; set; } = string.Empty;
    public string T24DistributionName { get; set; } = string.Empty;
}

public class DebitResponse
{
    public bool ResponseStatus { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public string ResponseData { get; set; } = string.Empty;
}

public class SettlementRequest
{
    public string TransactionReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string PaymentGateway { get; set; } = string.Empty;
}