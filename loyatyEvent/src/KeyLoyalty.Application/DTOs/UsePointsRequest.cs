namespace KeyLoyalty.Application.DTOs;

public class UsePointsRequest
{
    public string UserIdOrAccount { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public int PointsToUse { get; set; }
    public decimal TransactionAmount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
}