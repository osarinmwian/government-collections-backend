namespace KeyLoyalty.Application.DTOs;

public class AssignPointsRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public int Points { get; set; }
    public string TransactionType { get; set; } = "TEST";
}