namespace GovernmentCollections.Service.Services.Remita.Gateway;

public interface IRemitaPaymentGatewayService
{
    Task<RemitaTransactionStatusResponse> VerifyTransactionAsync(string transactionId);
    string GenerateCheckoutHash(RemitaCheckoutRequest request);
}