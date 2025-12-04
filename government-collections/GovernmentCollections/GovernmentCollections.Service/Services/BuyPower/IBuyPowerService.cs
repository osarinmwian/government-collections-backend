namespace GovernmentCollections.Service.Services.BuyPower;

public interface IBuyPowerService
{
    Task<dynamic> ProcessPaymentAsync(object request);
    Task<dynamic> VerifyTransactionAsync(string transactionId);
}