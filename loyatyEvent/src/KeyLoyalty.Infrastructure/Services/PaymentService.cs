using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace KeyLoyalty.Infrastructure.Services;

public interface IPaymentService
{
    Task<string> CreditCustomerAccountAsync(string accountNumber, decimal amount, string reference);
    Task<string> PurchaseAirtimeAsync(string phoneNumber, decimal amount, string telco, string reference);
    Task<string> PayBillAsync(string customerId, string billerCode, string paymentCode, decimal amount, string reference);
}

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;

    public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> CreditCustomerAccountAsync(string accountNumber, decimal amount, string reference)
    {
        var omniChannelUrl = _configuration["KeyLoyalty:OmniChannel:BaseUrl"];
        var loyaltyAccount = _configuration["KeyLoyalty:OmniChannel:LoyaltyAccount"];
        
        var ftRequest = new
        {
            DrAccountNo = loyaltyAccount,
            CrAccountNo = accountNumber,
            Amount = amount,
            RequestId = reference,
            Source = "LOYALTY",
            Narration = "Loyalty Points Redemption",
            TransactionType = "Own Account",
            SaveBeneficiary = false,
            AuthRequest = new
            {
                TPin = "",
                CardAccountNumber = loyaltyAccount
            }
        };

        var json = JsonSerializer.Serialize(ftRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{omniChannelUrl}/Transactions/IntraFundTransfer", content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            var ftResponse = JsonSerializer.Deserialize<FTResponse>(result);
            
            if (ftResponse?.status == "00")
            {
                _logger.LogInformation("Account {AccountNumber} credited with ₦{Amount} - T24 Ref: {T24Ref}", 
                    accountNumber, amount, ftResponse.id);
                return ftResponse.id;
            }
            
            _logger.LogError("T24 transfer failed for {AccountNumber}: {Message}", accountNumber, ftResponse?.responsemessage);
            throw new Exception($"Payment failed: {ftResponse?.responsemessage}");
        }
        
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to credit account {AccountNumber}: {Error}", accountNumber, error);
        throw new Exception($"Payment failed: {error}");
    }

    public async Task<string> PurchaseAirtimeAsync(string phoneNumber, decimal amount, string telco, string reference)
    {
        var omniChannelUrl = _configuration["KeyLoyalty:OmniChannel:BaseUrl"];
        var loyaltyAccount = _configuration["KeyLoyalty:OmniChannel:LoyaltyAccount"];
        
        var airtimeRequest = new
        {
            PhoneNumber = phoneNumber,
            Amount = amount,
            Network = telco,
            RequestId = reference,
            AccountNumber = loyaltyAccount,
            Source = "LOYALTY"
        };

        var json = JsonSerializer.Serialize(airtimeRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await _httpClient.PostAsync($"{omniChannelUrl}/Transactions/Airtime", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Airtime purchase successful for {PhoneNumber} - Amount: ₦{Amount}", phoneNumber, amount);
                return reference;
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Airtime purchase failed for {PhoneNumber}: {Error}", phoneNumber, error);
            
            // Automatically rollback loyalty points on failure
            await RollbackLoyaltyPointsAsync(reference, "Airtime purchase failed");
            throw new Exception($"Airtime purchase failed: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Airtime purchase exception for {PhoneNumber}", phoneNumber);
            await RollbackLoyaltyPointsAsync(reference, ex.Message);
            throw;
        }
    }

    public async Task<string> PayBillAsync(string customerId, string billerCode, string paymentCode, decimal amount, string reference)
    {
        var omniChannelUrl = _configuration["KeyLoyalty:OmniChannel:BaseUrl"];
        var loyaltyAccount = _configuration["KeyLoyalty:OmniChannel:LoyaltyAccount"];
        
        var billRequest = new
        {
            CustomerId = customerId,
            BillerCode = billerCode,
            PaymentCode = paymentCode,
            Amount = amount,
            RequestId = reference,
            AccountNumber = loyaltyAccount,
            Source = "LOYALTY"
        };

        var json = JsonSerializer.Serialize(billRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await _httpClient.PostAsync($"{omniChannelUrl}/Transactions/BillPayment", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Bill payment successful for {CustomerId} - Amount: ₦{Amount}", customerId, amount);
                return reference;
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Bill payment failed for {CustomerId}: {Error}", customerId, error);
            
            // Automatically rollback loyalty points on failure
            await RollbackLoyaltyPointsAsync(reference, "Bill payment failed");
            throw new Exception($"Bill payment failed: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bill payment exception for {CustomerId}", customerId);
            await RollbackLoyaltyPointsAsync(reference, ex.Message);
            throw;
        }
    }

    private async Task RollbackLoyaltyPointsAsync(string transactionReference, string reason)
    {
        try
        {
            var loyaltyServiceUrl = _configuration["KeyLoyalty:ServiceUrl"];
            var confirmRequest = new
            {
                RedemptionId = transactionReference,
                TransactionId = transactionReference,
                IsSuccessful = false,
                FailureReason = reason
            };

            var json = JsonSerializer.Serialize(confirmRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            await _httpClient.PostAsync($"{loyaltyServiceUrl}/api/loyalty/confirm-transaction", content);
            _logger.LogInformation("Loyalty points rollback initiated for transaction {TransactionReference}", transactionReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback loyalty points for transaction {TransactionReference}", transactionReference);
        }
    }
}

public class FTResponse
{
    public string status { get; set; } = string.Empty;
    public string responsemessage { get; set; } = string.Empty;
    public string id { get; set; } = string.Empty;
    public string draccountno { get; set; } = string.Empty;
    public string craccountno { get; set; } = string.Empty;
}