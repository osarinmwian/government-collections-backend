namespace KeyLoyalty.Application.DTOs
{
    public class ConfirmTransactionRequest
    {
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
    }
}