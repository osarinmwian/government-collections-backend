using Microsoft.Extensions.Configuration;

namespace GovernmentCollections.Service.Services.RevPay;

public class RevPayService : IRevPayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public RevPayService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<dynamic> ProcessPaymentAsync(object request)
    {
        return new { Status = "SUCCESS", Message = "RevPay payment processed" };
    }

    public async Task<dynamic> VerifyTransactionAsync(string transactionId)
    {
        return new { Status = "SUCCESS", TransactionId = transactionId, Message = "Transaction verified" };
    }
}