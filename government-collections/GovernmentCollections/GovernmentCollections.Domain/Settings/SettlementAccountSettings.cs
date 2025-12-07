namespace GovernmentCollections.Domain.Settings;

public class SettlementAccountSettings
{
    public string SettlementCreditAccount { get; set; } = string.Empty;
    public string CommissionCode { get; set; } = string.Empty;
    public string T24TransactionType { get; set; } = string.Empty;
    public string T24DistributionName { get; set; } = string.Empty;
    public Dictionary<string, decimal>? Commissions { get; set; }
}

public class FundTransferApiUrl
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}