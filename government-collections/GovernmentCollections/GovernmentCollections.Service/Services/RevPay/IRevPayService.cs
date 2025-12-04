namespace GovernmentCollections.Service.Services.RevPay;

public interface IRevPayService
{
    Task<dynamic> ProcessPaymentAsync(object request);
    Task<dynamic> VerifyTransactionAsync(string transactionId);
}