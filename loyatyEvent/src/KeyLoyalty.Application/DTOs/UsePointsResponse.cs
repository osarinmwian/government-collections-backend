namespace KeyLoyalty.Application.DTOs;

public class UsePointsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AvailablePoints { get; set; }
    public int PointsUsed { get; set; }
    public int RemainingPoints { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
}