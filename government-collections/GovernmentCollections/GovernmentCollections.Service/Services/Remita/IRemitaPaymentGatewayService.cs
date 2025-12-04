namespace GovernmentCollections.Service.Services.Remita;

public interface IRemitaPaymentGatewayService
{
    Task<RemitaTransactionStatusResponse> VerifyTransactionAsync(string transactionId);
    string GenerateCheckoutHash(RemitaCheckoutRequest request);
}